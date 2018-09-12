using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SharpLink.Events;

namespace RiasBot.Services.WebSockets
{
    public class VotesWebSocket
    {
        private bool _connected;
        private ClientWebSocket _webSocket;
        private readonly Uri _hostUri;
        private readonly VotesManagerConfig _config;

        public event AsyncEvent<JObject> OnReceive;

        public event AsyncEvent<WebSocketCloseStatus?, string> OnClosed;

        public VotesWebSocket(VotesManagerConfig config)
        {
            _config = config;
            var connectionType = config.IsSecureConnection ? "wss" : "ws";
            _hostUri = new Uri($"{connectionType}://{config.WebSocketHost}:{config.WebSocketPort}/{config.UrlParameters}");
        }

        private async Task ConnectWebSocketAsync()
        {
            _webSocket = new ClientWebSocket();
            _webSocket.Options.SetRequestHeader("Authorization", _config.Authorization);
            
            await _webSocket.ConnectAsync(_hostUri, CancellationToken.None);
            Console.WriteLine($"{DateTime.UtcNow:MMM dd hh:mm:ss} VotesWebSocket connected!");
            _connected = true;
            while (_webSocket.State == WebSocketState.Open)
            {
                var jsonString = await ReceiveAsync(_webSocket);
                var json = JObject.Parse(jsonString);
                // ISSUE: reference to a compiler-generated field
                if (OnReceive != null) await OnReceive.InvokeAsync(json);
            }
        }

        private async Task DisconnectWebSocketAsync()
        {
            if (_webSocket == null)
                return;
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
        }

        private async Task<string> ReceiveAsync(ClientWebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            var end = false;
            while (!end)
            {
                var socketReceiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                var result = socketReceiveResult;
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    // ISSUE: reference to a compiler-generated field
                    var onClosed = OnClosed;
                    if (onClosed != null)
                        await onClosed.InvokeAsync(result.CloseStatus, result.CloseStatusDescription);
                    _connected = false;
                    Console.WriteLine($"{DateTime.UtcNow:MMM dd hh:mm:ss} VotesWebSocket disconnected!");
                }
                else
                {
                    if (result.EndOfMessage)
                        end = true;
                }
            }
            return Encoding.UTF8.GetString(buffer);
        }

        public bool IsConnected()
        {
            return _webSocket != null && _connected;
        }

        public async Task SendAsync(string message)
        {
            var messageBytes = Encoding.UTF8.GetBytes(message);
            await _webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public async Task Connect()
        {
            await Task.Factory.StartNew(async () => await ConnectWebSocketAsync());
        }

        public async Task Disconnect()
        {
            await Task.Factory.StartNew(async () => await DisconnectWebSocketAsync());
        }

        public string GetHostUri()
        {
            return _hostUri.AbsoluteUri;
        }
    }
}