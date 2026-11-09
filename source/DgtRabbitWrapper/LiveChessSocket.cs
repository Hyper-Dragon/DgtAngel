using Fleck;
using System.Threading.Tasks;

namespace DgtRabbitWrapper
{
    /// <summary>
    /// The only part of a connected LiveChess client the server uses: where it is and how
    /// to write to it.
    /// </summary>
    /// <remarks>
    /// Exists so the transmission path can be tested without standing up a Fleck listener.
    /// Fleck's <see cref="IWebSocketConnection"/> can only be obtained from a real socket.
    /// </remarks>
    public interface ILiveChessSocket
    {
        int ClientPort { get; }

        Task Send(string message);
    }

    /// <summary>Adapts a Fleck connection to <see cref="ILiveChessSocket"/>.</summary>
    internal sealed class FleckLiveChessSocket : ILiveChessSocket
    {
        private readonly IWebSocketConnection _connection;

        public FleckLiveChessSocket(IWebSocketConnection connection) => _connection = connection;

        public int ClientPort => _connection.ConnectionInfo.ClientPort;

        public Task Send(string message) => _connection.Send(message);
    }
}
