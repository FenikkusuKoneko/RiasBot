using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Rias.Core.Commons.Configs;

namespace Rias.Core.Services.WebSocket
{
    public class RiasWebsocket
    {
        private ClientWebSocket? _webSocket;
        
        private IWebsocketConfig? _config;
        private Uri? _hostUri;
        
        public event Func<Task>? OnConnected;
        public event Func<WebSocketCloseStatus?, string, Task>? OnDisconnected;
        public event Func<string, Task>? OnReceive;
        public event Func<LogMessage, Task>? Log;

        public Task ConnectAsync(IWebsocketConfig config)
        {
            _config = config;
            
            var connectionType = config.IsSecureConnection ? "wss" : "ws";
            _hostUri = new Uri($"{connectionType}://{config.WebSocketHost}:{config.WebSocketPort}/{config.UrlParameters}");
            
            _ = Task.Run(async () => await TryConnectAsync());
            return Task.CompletedTask;
        }
        
        public Task Disconnect()
        {
            _ = Task.Run(async () => await DisconnectWebSocketAsync());
            return Task.CompletedTask;
        }

        private async Task TryConnectAsync()
        {
            while (!IsConnected())
            {
                try
                {
                    await InternalConnectAsync();
                }
                catch
                {
                    if (Log != null)
                        await Log.Invoke(new LogMessage(LogSeverity.Warning, "Websocket", "The websocket was closed or aborted! Attempting reconnect in 1 minute"));
                    
                    await Task.Delay(TimeSpan.FromMinutes(1));
                    await TryConnectAsync();
                    break;
                }
            }
        }

        private async Task InternalConnectAsync()
        {
            _webSocket = new ClientWebSocket();
            _webSocket.Options.SetRequestHeader("Authorization", _config!.Authorization);
            
            await _webSocket.ConnectAsync(_hostUri, CancellationToken.None);
            
            if (OnConnected != null) await OnConnected.Invoke();
            while (_webSocket.State == WebSocketState.Open)
            {
                var data = await ReceiveAsync();
                if (OnReceive != null) await OnReceive.Invoke(data);
            }
            
            if (Log != null)
                await Log.Invoke(new LogMessage(LogSeverity.Error, "Websocket", "The websocket was closed or aborted! Attempting reconnect in 1 minute"));
            
            var unused = new Timer(async _ => await TryConnectAsync(), null, TimeSpan.FromMinutes(1), TimeSpan.Zero);
        }
        
        private async Task DisconnectWebSocketAsync()
        {
            if (_webSocket == null)
                return;
            
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            if (OnDisconnected != null) await OnDisconnected.Invoke(WebSocketCloseStatus.NormalClosure, "Closed normally");
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
                    if (OnDisconnected != null)
                        await OnDisconnected.Invoke(result.CloseStatus, result.CloseStatusDescription);
                }
                else
                {
                    if (result.EndOfMessage)
                        end = true;
                }
            }
            
            return Encoding.UTF8.GetString(buffer);
        }
        
        private bool IsConnected()
        {
            return _webSocket != null && _webSocket.State == WebSocketState.Open;
        }
    }
}