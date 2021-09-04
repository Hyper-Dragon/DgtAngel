using DgtAngelShared.Json;
using DgtCherub.Services;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using static DgtAngelShared.Json.CherubApiMessage;

namespace DgtCherub.Controllers
{
    public sealed class CherubWebSocketApiController : ControllerBase
    {
        private const int RECIEVE_BUFFER_SIZE_BYTES = 1024 * 10;
        private bool isAcceptingConnections = true;
        private static WebSocket runningSocket = null;

        private readonly ILogger _logger;
        private readonly IAngelHubService _appDataService;

        public CherubWebSocketApiController(ILogger<CherubWebSocketApiController> logger, IAngelHubService appData)
        {
            _logger = logger;
            _appDataService = appData;
        }

        [HttpGet("/ws")]
        public async Task Get()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest && isAcceptingConnections)
            {
                //Only allow one connection 
                if (runningSocket != null)
                {
                    runningSocket.Abort();
                }

                runningSocket = null;

                using WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                runningSocket = webSocket;

                _appDataService.UserMessageArrived("WSAPI", "DGT Angel has Connected");

                long highestCaptureTimeRecieved = int.MinValue;

                while (webSocket.State == WebSocketState.Open && isAcceptingConnections)
                {
                    try
                    {
                        ArraySegment<byte> buffer = new(new byte[RECIEVE_BUFFER_SIZE_BYTES]);
                        WebSocketReceiveResult result;
                        List<byte> allBytes = new();

                        do
                        {
                            //TODO: Make read more efficient and use the canx token on shutdown
                            result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                            for (int i = 0; i < result.Count; i++)
                            {
                                allBytes.Add(buffer.Array[i]);
                            }
                        }
                        while (!result.EndOfMessage);

                        if (allBytes.Count == 0)
                        {
                            // The remote connection has been forcefully terminated
                            break;
                        }

                        try
                        {
                            // Convert to a string (UTF-8 encoding).
                            CherubApiMessage messageIn = JsonSerializer.Deserialize<CherubApiMessage>(Encoding.UTF8.GetString(allBytes.ToArray(), 0, allBytes.Count));


                            switch (messageIn.MessageType)
                            {
                                case CherubApiMessage.MessageTypeCode.STATE_UPDATED:
                                    if (messageIn.RemoteBoard.CaptureTimeMs > highestCaptureTimeRecieved)
                                    {
                                        highestCaptureTimeRecieved = messageIn.RemoteBoard.CaptureTimeMs;

                                        switch (messageIn.RemoteBoard.State.Code)
                                        {
                                            case ResponseCode.LOST_VISABILITY:
                                            case ResponseCode.MOVE_LIST_MISSING:
                                                _appDataService.UserMessageArrived("INGEST", $"Make sure that the game windows is part visible on the screen!");
                                                break;
                                            case ResponseCode.SCRIPT_SCRAPE_ERROR:
                                                _appDataService.UserMessageArrived("INGEST", $"It looks like the page may have changed...please raise a bug report.");
                                                break;
                                            case ResponseCode.UNKNOWN_PAGE:
                                                _appDataService.UserMessageArrived("INGEST", $"Trying to parse an unknown page...please raise a bug report.");
                                                break;
                                            case ResponseCode.GAME_PENDING:
                                            case ResponseCode.GAME_IN_PROGRESS:
                                            case ResponseCode.GAME_COMPLETED:
                                                _logger?.LogTrace($"{messageIn.RemoteBoard.CaptureTimeMs} Fen:{messageIn.RemoteBoard.Board.FenString}");
                                                _appDataService.RemoteBoardUpdated(messageIn.RemoteBoard);
                                                break;
                                            default:
                                                _appDataService.UserMessageArrived("INGEST", $"Unhandled status code of [{messageIn.RemoteBoard.State.Code}] arrived.");
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        _appDataService.UserMessageArrived("INGEST", $"ERROR: Message out of sequence recieved from Angel...dropping");
                                    }
                                    break;
                                case CherubApiMessage.MessageTypeCode.KEEP_ALIVE:
                                    _logger?.LogTrace("Keep Alive PING Arrived");
                                    await webSocket.SendAsync(Encoding.UTF8.GetBytes("PONG"),
                                                              WebSocketMessageType.Text,
                                                              true,
                                                              CancellationToken.None);
                                    _logger?.LogTrace("Keep Alive PONG Sent");
                                    break;
                                case CherubApiMessage.MessageTypeCode.MESSAGE:
                                    _appDataService.UserMessageArrived(messageIn.Source, messageIn.Message);
                                    break;
                                case CherubApiMessage.MessageTypeCode.WATCH_STARTED:
                                    _appDataService.WatchStateChange(MessageTypeCode.WATCH_STARTED, messageIn.RemoteBoard);
                                    _appDataService.UserMessageArrived(messageIn.Source, $"DGT Angel started watching a remote board.");
                                    break;
                                case CherubApiMessage.MessageTypeCode.WATCH_STOPPED:
                                    _appDataService.WatchStateChange(MessageTypeCode.WATCH_STOPPED, messageIn.RemoteBoard);
                                    _appDataService.UserMessageArrived(messageIn.Source, $"DGT Angel stopped watching the remote board.");
                                    break;
                                default:
                                    _appDataService.UserMessageArrived("INTERNAL", "ERROR: Unknown MESSAGE TYPE recieved on the API!");
                                    _logger?.LogError($"Unknown MESSAGE TYPE recieved on the API!");
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            _appDataService.UserMessageArrived("INTERNAL", $"ERROR: Unknown DATA recieved on the API :: {ex.Message}");
                            _logger?.LogError($"Unknown DATA recieved on the API :: {ex.Message}");
                        }
                    }
                    catch (ConnectionAbortedException)
                    {
                        // The Web Socket API Stopped...Cherub is shutting down.
                        isAcceptingConnections = false;
                    }
                }

                _appDataService.WatchStateChange(MessageTypeCode.WATCH_STOPPED);
                _appDataService.UserMessageArrived("INTERNAL", "DGT Angel has Disconnected");
            }
            else
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }
    }
}
