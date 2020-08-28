using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Rias.Core.Configuration;
using Serilog;

namespace Rias.Core.Services
{
    public class WebSocketClient
    {
        public event Func<string, Task>? DataReceived;
        public event Func<Task>? Closed;
        
        private ClientWebSocket? _webSocket;
        private IWebSocketConfiguration _configuration;
        private readonly Uri _url;

        public WebSocketClient(IWebSocketConfiguration configuration)
        {
            _configuration = configuration;
            var connectionType = configuration.IsSecureConnection ? "wss" : "ws";
            _url = new Uri($"{connectionType}://{configuration.WebSocketHost}:{configuration.WebSocketPort}/{configuration.UrlParameters}");
        }

        public async Task ConnectAsync()
        {
            _webSocket?.Dispose();
            _webSocket = new ClientWebSocket();
            _webSocket.Options.SetRequestHeader("Authorization", _configuration.Authorization);
            
            await _webSocket.ConnectAsync(_url, CancellationToken.None);
            _ = Task.Run(ReceiveAsync);
        }

        public async Task SendAsync(string data)
        {
            if (_webSocket is null)
                return;
            
            if (_webSocket.State != WebSocketState.Open)
                return;
            
            var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(data));

            try
            {
                await _webSocket.SendAsync(buffer, WebSocketMessageType.Binary, true, CancellationToken.None);
            }
            catch
            {
            }
        }

        private async Task ReceiveAsync()
        {
            var bytes = new byte[4096];
            var buffer = new ArraySegment<byte>(bytes);

            try
            {
                while (_webSocket!.State == WebSocketState.Open)
                {
                    var result = await _webSocket.ReceiveAsync(buffer, CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        try
                        {
                            await _webSocket.CloseAsync(_webSocket.CloseStatus.GetValueOrDefault(), _webSocket.CloseStatusDescription, CancellationToken.None);
                        }
                        catch
                        {
                        }
                        
                        Closed?.Invoke();
                        return;
                    }

                    DataReceived?.Invoke(Encoding.UTF8.GetString(bytes, 0, result.Count));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception in RiasBot WebSocketClient");
                Closed?.Invoke();
            }
        }
    }
}