using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DgtCherubTest.TestSupport
{
    /// <summary>
    /// A websocket whose inbound frames the test controls and whose outbound frames it
    /// records. When the queue empties it reports a zero-length close frame, so the
    /// controller's read loop terminates instead of hanging the test.
    /// </summary>
    internal sealed class FakeWebSocket : WebSocket
    {
        private readonly Queue<byte[]> _inbound = new();
        private WebSocketState _state = WebSocketState.Open;

        public List<string> Sent { get; } = new();

        public override WebSocketState State => _state;

        public override string? SubProtocol => null;

        public override WebSocketCloseStatus? CloseStatus => null;

        public override string? CloseStatusDescription => null;

        /// <summary>Queues a text frame exactly as the browser extension would send it.</summary>
        public void QueueText(string message) => _inbound.Enqueue(Encoding.UTF8.GetBytes(message));

        /// <summary>Queues the zero-length frame the extension's connection teardown produces.</summary>
        public void QueueDisconnect() => _inbound.Enqueue(Array.Empty<byte>());

        public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            if (_inbound.Count == 0)
            {
                _state = WebSocketState.CloseReceived;
                return Task.FromResult(new WebSocketReceiveResult(0, WebSocketMessageType.Close, true));
            }

            byte[] frame = _inbound.Dequeue();
            frame.CopyTo(buffer.Array!, 0);

            return Task.FromResult(new WebSocketReceiveResult(
                frame.Length,
                frame.Length == 0 ? WebSocketMessageType.Close : WebSocketMessageType.Text,
                endOfMessage: true));
        }

        public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            Sent.Add(Encoding.UTF8.GetString(buffer.AsSpan()));
            return Task.CompletedTask;
        }

        public override void Abort() => _state = WebSocketState.Aborted;

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            _state = WebSocketState.Closed;
            return Task.CompletedTask;
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            _state = WebSocketState.Closed;
            return Task.CompletedTask;
        }

        public override void Dispose() => _state = WebSocketState.Closed;
    }

    /// <summary>Makes <see cref="FakeWebSocket"/> the socket a DefaultHttpContext hands out.</summary>
    internal sealed class FakeWebSocketFeature : IHttpWebSocketFeature
    {
        public FakeWebSocketFeature(FakeWebSocket socket, bool isWebSocketRequest = true)
        {
            Socket = socket;
            IsWebSocketRequest = isWebSocketRequest;
        }

        public FakeWebSocket Socket { get; }

        public bool IsWebSocketRequest { get; }

        public Task<WebSocket> AcceptAsync(WebSocketAcceptContext context) => Task.FromResult<WebSocket>(Socket);
    }
}
