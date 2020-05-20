using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Disqord.Logging;
using Rias.Core.Configuration;

namespace Rias.Core.Services.WebSocket
{
    public class RiasWebSocket
    {
        private ClientWebSocket? _webSocket;

        private readonly IWebsocketConfiguration _config;
        private readonly Uri? _hostUri;
        private readonly string _source;
        
        public event Func<MessageLoggedEventArgs, Task>? Log;
        public event Func<string, Task>? DataReceived;

        public RiasWebSocket(IWebsocketConfiguration config, string source)
        {
            _config = config;
            var connectionType = config.IsSecureConnection ? "wss" : "ws";
            _hostUri = new Uri($"{connectionType}://{config.WebSocketHost}:{config.WebSocketPort}/{config.UrlParameters}");
            _source = source;
        }

        public async Task ConnectAsync()
        {
            _webSocket = new ClientWebSocket();
            _webSocket.Options.SetRequestHeader("Authorization", _config.Authorization);
            
            try
            {
                await _webSocket.ConnectAsync(_hostUri, CancellationToken.None);
                if (Log != null)
                    await Log.Invoke(new MessageLoggedEventArgs(_source, LogMessageSeverity.Information, "WebSocket connected"));

                while (_webSocket.State == WebSocketState.Open)
                {
                    var data = await ReceiveAsync();
                    if (DataReceived != null)
                        await DataReceived.Invoke(data);
                }
            }
            catch
            {
                //ignored
            }

            _webSocket.Dispose();
            if (Log != null)
                await Log.Invoke(new MessageLoggedEventArgs(_source, LogMessageSeverity.Warning, "The WebSocket was closed or aborted! Attempting reconnect in 1 minute"));

            await Task.Delay(TimeSpan.FromMinutes(1));
            _ = Task.Run(async () => await ConnectAsync());
        }

        private async Task<string> ReceiveAsync()
        {
            var buffer = new byte[4 * 1024];
            var end = false;
            while (!end)
            {
                var result = await _webSocket!.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    if (Log != null)
                        await Log.Invoke(new MessageLoggedEventArgs(_source, LogMessageSeverity.Warning,
                            "The WebSocket is closed.\n" +
                            $"Close status: {result.CloseStatus}\n" +
                            $"Description: {result.CloseStatusDescription}"));
                }
                else
                {
                    if (result.EndOfMessage)
                        end = true;
                }
            }
            
            return Encoding.UTF8.GetString(buffer);
        }
    }
}