using DgtAngelShared.Json;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static DgtAngelShared.Json.CherubApiMessage;

namespace DgtAngel.Services
{
    public class ConnectionManagerEventArgs : EventArgs
    {
        public string ResponseOut { get; set; }
    }

    public interface IConnectionManager
    {
        event EventHandler<ConnectionManagerEventArgs> OnCherubConnected;
        event EventHandler<ConnectionManagerEventArgs> OnCherubDisconnected;
        event EventHandler<ConnectionManagerEventArgs> OnError;

        Task ConnectAndWatch();
        Task SendDgtAngelDisconnectedToClient();
        Task SendDgtAngelConnectedToClient();
        Task SendMessageToClient(string message);
        Task SendUpdatedBoardStateToClient(BoardState remoteBoardState);
        Task StartAndManageConnection();
    }

    public sealed class CherubConnectionManager : IConnectionManager
    {
        private const string CHERUB_API_WS_HOST = "ws://localhost:37964";
        private const string CHERUB_API_WS_PATH = "/ws";
        private const string CHERUB_API_HTTP_HOST = "http://127.0.0.1:37964";
        private const string CHERUB_API_HTTP_PATH = "/api";
        private const string CHERUB_API_HTTP_TEST_CALL = "/AreYouThere";
        private const int CONNECTION_RETRY_DELAY = 1000;
        private const int CONNECTION_KEEPALIVE_DELAY = 60000;

        public event EventHandler<ConnectionManagerEventArgs> OnCherubConnected;
        public event EventHandler<ConnectionManagerEventArgs> OnCherubDisconnected;
        public event EventHandler<ConnectionManagerEventArgs> OnError;

        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;
        private ClientWebSocket _socket = null;

        private string replayOnConnectMessage = "";


        private readonly string keepAliveMessage = JsonSerializer.Serialize<CherubApiMessage>(new CherubApiMessage()
        {
            Source = "ANGEL",
            MessageType = MessageTypeCode.KEEP_ALIVE,
            Message = "",
            RemoteBoard = null
        });

        public CherubConnectionManager(ILogger<CherubConnectionManager> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        private async Task WaitForCherub()
        {
            _logger?.LogInformation($"Looking for Cherub...");
            for (; ; )
            {
                try
                {
                    HttpResponseMessage apiResponse = await _httpClient.GetAsync($"{CHERUB_API_HTTP_HOST}{CHERUB_API_HTTP_PATH}{CHERUB_API_HTTP_TEST_CALL}");

                    if (apiResponse.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        _logger?.LogInformation("Found Cherub");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogDebug($"Cherub not found (Expect - TypeError: Failed to fetch) :: {ex.Message}");
                }

                //Wait and then try again
                await Task.Delay(CONNECTION_RETRY_DELAY);
            }
        }

        public async Task StartAndManageConnection()
        {
            _logger?.LogInformation($"Starting Chrub Connection Manager");

            for (; ; )
            {
                //Do this to avoid uncatchable web socket errors generated in the chrome console
                await WaitForCherub();

                try
                {
                    _logger?.LogInformation("Connecting to Cherub /ws");
                    await ConnectAndWatch();
                }
                catch (WebSocketException ex)
                {
                    _logger?.LogInformation($"Cherub Connection (Websocket) :: {ex.Message}");
                    //OnCherubDisconnected?.Invoke(this, new CherubConnectionEventArgs() { ResponseOut = $"Connection to Cherub Unavailable ({ex.WebSocketErrorCode})" });
                }
                catch (TaskCanceledException ex)
                {
                    _logger?.LogInformation($"Cherub Connection (Task Canx) :: {ex.Message}");
                }
                catch (InvalidOperationException ex)
                {
                    //OnCherubDisconnected?.Invoke(this, new CherubConnectionEventArgs() { ResponseOut = $"Connection to Cherub Unavailable (Terminated by Invalid Opperation)" });
                    _logger?.LogInformation($"Cherub Connection (Opperation) :: {ex.Message}");
                }
                catch (Exception ex)
                {
                    //OnError?.Invoke(this, new CherubConnectionEventArgs() { ResponseOut = $" ERROR: {ex.GetType()}{ex.Message}" });
                    _logger?.LogInformation($"Cherub Connection (Unknown) :: {ex.Message}");
                }
                finally
                {
                    OnCherubDisconnected?.Invoke(this, new ConnectionManagerEventArgs() { ResponseOut = "Disconnected from Cherub" });
                    _socket = null;
                }
            }
        }

        public async Task ConnectAndWatch()
        {
            //Open a websocket to DGT Cherub (running on the local machine)
            _socket = new();

            do
            {
                await _socket.ConnectAsync(new Uri($"{CHERUB_API_WS_HOST}{CHERUB_API_WS_PATH}"), CancellationToken.None).ConfigureAwait(false);
            } while (_socket.State != WebSocketState.Open);

            if (!string.IsNullOrWhiteSpace(replayOnConnectMessage))
            {
                await SendJsonToClient(replayOnConnectMessage);
            }

            OnCherubConnected?.Invoke(this, new ConnectionManagerEventArgs() { ResponseOut = "Connected to Cherub" });

            // Send PING/PONG messages to keep the socket alive/detect disconnects
            for (; ; )
            {
                await SendJsonToClient(keepAliveMessage);

                using (CancellationTokenSource canxTokenSource = new(2000))
                {
                    ArraySegment<byte> buffer = new(new byte[1024 * 4]);
                    WebSocketReceiveResult test = await _socket.ReceiveAsync(buffer, canxTokenSource.Token);
                    _logger.LogDebug($"Keep Alive Bytes Recieved {test.Count} : Expected 4");
                }

                await Task.Delay(CONNECTION_KEEPALIVE_DELAY);
            }
        }

        // These method are fire and forget - if Cherub isn't there that's fine - just log
        private async Task SendJsonToClient(string message, bool saveAsLastMessage = false)
        {
            // Save message to replay 
            if (saveAsLastMessage) { replayOnConnectMessage = message; }

            try
            {
                if (_socket != null && _socket.State == WebSocketState.Open)
                {
                    await _socket.SendAsync(Encoding.UTF8.GetBytes(message),
                                            WebSocketMessageType.Text,
                                            true,
                                            CancellationToken.None);
                }
            }
            catch (Exception ex) { _logger?.LogInformation($"Failed to send to Cherub>> {_socket?.State} :: {ex.Message}"); }
        }

        public async Task SendMessageToClient(string message)
        {
            _logger.LogDebug($"Sending Message {message} to Client");
            await SendJsonToClient(JsonSerializer.Serialize<CherubApiMessage>(new CherubApiMessage()
            {
                Source = "ANGEL",
                MessageType = MessageTypeCode.MESSAGE,
                Message = message,
                RemoteBoard = null
            }));
        }

        public async Task SendUpdatedBoardStateToClient(BoardState remoteBoardState)
        {
            _logger.LogDebug($"Sending FEN [{remoteBoardState.Board.FenString}] to Client");
            await SendJsonToClient(JsonSerializer.Serialize<CherubApiMessage>(new CherubApiMessage()
            {
                Source = "ANGEL",
                MessageType = MessageTypeCode.STATE_UPDATED,
                Message = "",
                RemoteBoard = remoteBoardState
            }), true);
        }

        public async Task SendDgtAngelConnectedToClient()
        {
            _logger.LogDebug($"Sending DGT Angel disconnect to Client");
            await SendJsonToClient(JsonSerializer.Serialize<CherubApiMessage>(new CherubApiMessage()
            {
                Source = "ANGEL",
                MessageType = MessageTypeCode.WATCH_STARTED,
                Message = "",
                RemoteBoard = null
            }));
        }

        public async Task SendDgtAngelDisconnectedToClient()
        {
            _logger.LogDebug($"Sending DGT Angel disconnect to Client");
            await SendJsonToClient(JsonSerializer.Serialize<CherubApiMessage>(new CherubApiMessage()
            {
                Source = "ANGEL",
                MessageType = MessageTypeCode.WATCH_STOPPED,
                Message = "",
                RemoteBoard = null
            }));
        }
    }
}

