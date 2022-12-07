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

        private const int MSG_LIMIT_SECONDS = 10;
        private DateTime lastErrorLog = DateTime.MinValue;


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
                bool isClientVersionDisplayed = false;
                bool isClientVersionOk = false;

                //Initial value set to non-error
                //Just used to prevent duplicate messages in the console
                ResponseCode lastErrorSeen = ResponseCode.RUNNING;

                //Only allow one connection 
                runningSocket?.Abort();

                runningSocket = null;

                using WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                runningSocket = webSocket;

                _appDataService.UserMessageArrived("WSAPI", "Angel has Connected");

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
                            string utfMessage = Encoding.UTF8.GetString(allBytes.ToArray(), 0, allBytes.Count);
                            CherubApiMessage messageIn = JsonSerializer.Deserialize<CherubApiMessage>(utfMessage);

                            if (!isClientVersionDisplayed)
                            {
                                _appDataService.UserMessageArrived("INGEST", $"Chrome Extension {messageIn.AngelPluginName}-{messageIn.AngelPluginVersion}");
                                _appDataService.UserMessageArrived("INGEST", $"       Extension Message Version  {messageIn.AngelMessageVersion}");
                                isClientVersionDisplayed = true;

                                if (messageIn.AngelMessageVersion == "3.0")
                                {
                                    isClientVersionOk = true;
                                }
                                else
                                {
                                    _appDataService.UserMessageArrived("INGEST", $"INCOMPATIBLE CHROME EXTENSION - PLEASE UPDATE");
                                }
                            }

                            //Keep the connection but don't process anything
                            if (!isClientVersionOk)
                            {
                                continue;
                            }

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
                                                if (DateTime.UtcNow > lastErrorLog.AddSeconds(MSG_LIMIT_SECONDS))
                                                {
                                                    lastErrorLog = DateTime.UtcNow;
                                                    _appDataService.UserMessageArrived("INGEST", $"Make sure that the game windows is part visible on the screen.");
                                                    _appDataService.UserMessageArrived("INGEST", $"Make sure that you are not in focus mode.");
                                                    _appDataService.WatchStateChange(MessageTypeCode.WATCH_STOPPED_MOVES_ONLY, messageIn.AngelPluginName);
                                                }

                                                break;
                                            case ResponseCode.SCRIPT_SCRAPE_ERROR:
                                                if (lastErrorSeen != ResponseCode.SCRIPT_SCRAPE_ERROR)
                                                {
                                                    lastErrorSeen = ResponseCode.SCRIPT_SCRAPE_ERROR;
                                                    _appDataService.UserMessageArrived("INGEST", $"ERROR: It looks like the page may have changed...please raise a bug report.");
                                                }

                                                break;
                                            case ResponseCode.PAGE_READ_ERROR:
                                                if (lastErrorSeen != ResponseCode.PAGE_READ_ERROR)
                                                {
                                                    lastErrorSeen = ResponseCode.PAGE_READ_ERROR;
                                                    _appDataService.UserMessageArrived("INGEST", $"ERROR: It looks like the page may have changed...please raise a bug report.");
                                                    _appDataService.UserMessageArrived("INGEST", $"From Angel [{messageIn.RemoteBoard.State.Message}]");
                                                }

                                                break;
                                            case ResponseCode.UNKNOWN_PAGE:
                                                if (lastErrorSeen != ResponseCode.UNKNOWN_PAGE)
                                                {
                                                    lastErrorSeen = ResponseCode.UNKNOWN_PAGE;
                                                    _appDataService.UserMessageArrived("INGEST", $"ERROR: From Angel [{messageIn.RemoteBoard.State.Message}]");
                                                }

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
                                    _appDataService.WatchStateChange(MessageTypeCode.WATCH_STARTED, messageIn.AngelPluginName, messageIn.RemoteBoard);
                                    _appDataService.UserMessageArrived(messageIn.Source, $"Angel started watching a remote board.");
                                    break;
                                case CherubApiMessage.MessageTypeCode.WATCH_STOPPED:
                                    _appDataService.WatchStateChange(MessageTypeCode.WATCH_STOPPED, messageIn.AngelPluginName, messageIn.RemoteBoard);
                                    _appDataService.UserMessageArrived(messageIn.Source, $"Angel stopped watching the remote board.");
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

                _appDataService.WatchStateChange(MessageTypeCode.WATCH_STOPPED,"");
                _appDataService.PluginDisconnect();
                _appDataService.UserMessageArrived("INTERNAL", "Angel has Disconnected");
            }
            else
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }
    }
}
