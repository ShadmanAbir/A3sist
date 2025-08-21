using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace A3sist.Orchastrator.Services
{
    public class WebSocketService
    {
        private readonly ConcurrentDictionary<string, WebSocket> _sockets = new ConcurrentDictionary<string, WebSocket>();
        private readonly ContextRouter _router;

        public WebSocketService(ContextRouter router)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
        }

        public async Task HandleWebSocketConnection(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                var socketId = Guid.NewGuid().ToString();

                _sockets.TryAdd(socketId, webSocket);

                try
                {
                    await Receive(socketId, webSocket, context.RequestAborted);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"WebSocket error: {ex.Message}");
                }
                finally
                {
                    _sockets.TryRemove(socketId, out _);
                }
            }
            else
            {
                context.Response.StatusCode = 400;
            }
        }

        private async Task Receive(string socketId, WebSocket webSocket, CancellationToken cancellationToken)
        {
            var buffer = new byte[1024 * 4];

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", cancellationToken);
                        break;
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"Received message: {message}");

                    // Process the message and route the context
                    await ProcessWebSocketMessage(socketId, message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket receive error: {ex.Message}");
            }
        }

        private async Task ProcessWebSocketMessage(string socketId, string message)
        {
            try
            {
                // In a real implementation, we would parse the message and route the context
                Console.WriteLine($"Processing WebSocket message from {socketId}: {message}");

                // For demonstration, we'll just echo the message back
                await SendMessageAsync(socketId, $"Echo: {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing WebSocket message: {ex.Message}");
                await SendMessageAsync(socketId, $"Error: {ex.Message}");
            }
        }

        public async Task SendMessageAsync(string socketId, string message)
        {
            if (_sockets.TryGetValue(socketId, out var webSocket))
            {
                if (webSocket.State == WebSocketState.Open)
                {
                    var bytes = Encoding.UTF8.GetBytes(message);
                    await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }

        public async Task SendMessageToAllAsync(string message)
        {
            foreach (var socket in _sockets)
            {
                if (socket.Value.State == WebSocketState.Open)
                {
                    await SendMessageAsync(socket.Key, message);
                }
            }
        }

        public int GetActiveConnectionCount()
        {
            return _sockets.Count;
        }
    }
}