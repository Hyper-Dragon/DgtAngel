using DgtAngelShared.Json;
using DgtCherub.Services;
using DgtEbDllWrapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DgtCherub.Controllers
{
    public sealed class CherubWebSocketApiController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IAppDataService _appDataService;
        private readonly IDgtEbDllFacade _dgtEbDllFacade;

        public CherubWebSocketApiController(ILogger<Form1> logger, IAppDataService appData, IDgtEbDllFacade dgtEbDllFacade)
        {
            _logger = logger;
            _appDataService = appData;
            _dgtEbDllFacade = dgtEbDllFacade;
        }

        [HttpGet("/ws")]
        public async Task Get()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using WebSocket webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                _appDataService.UserMessageArrived("INTERNAL", "DGT Angel Connected");

                while (webSocket.State == WebSocketState.Open)
                {
                    ArraySegment<byte> buffer = new(new byte[1024]);
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

                    try
                    {
                        // Convert to a string (UTF-8 encoding).
                        CherubApiMessage messageIn = JsonSerializer.Deserialize<CherubApiMessage>(Encoding.UTF8.GetString(allBytes.ToArray(), 0, allBytes.Count));

                        switch (messageIn.MessageType)
                        {
                            case CherubApiMessage.MessageTypeCode.MESSAGE:
                                _appDataService.UserMessageArrived(messageIn.Source, messageIn.Message);
                                break;
                            case CherubApiMessage.MessageTypeCode.WATCH_STOPPED:
                                _appDataService.ResetChessDotComRemoteBoardState();
                                _appDataService.UserMessageArrived(messageIn.Source, $"DGT Angel stopped watching the remote board.");
                                break;
                            case CherubApiMessage.MessageTypeCode.KEEP_ALIVE:
                                _logger.LogDebug($"Cherub Keep-Alive from {messageIn.Source}");
                                //_appDataService.UserMessageArrived(messageIn.Source, "Keep Alive PING Arrived");

                                await webSocket.SendAsync(Encoding.UTF8.GetBytes("PONG"),
                                                          WebSocketMessageType.Text,
                                                          true,
                                                          CancellationToken.None);

                                //_appDataService.UserMessageArrived(messageIn.Source, "Keep Alive PONG Sent");

                                break;
                            case CherubApiMessage.MessageTypeCode.STATE_UPDATED:
                                _appDataService.UserMessageArrived(messageIn.Source, messageIn.RemoteBoard.Board.FenString);

                                // Account for the actual time captured/now if clock running
                                var captureTimeDiffMs = (int)((DateTime.Now.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))).TotalMilliseconds - ((double)messageIn.RemoteBoard.CaptureTimeMs));
                                TimeSpan whiteTimespan = new(0, 0, 0, 0, messageIn.RemoteBoard.Board.Clocks.WhiteClock - ((messageIn.RemoteBoard.Board.Turn == TurnCode.WHITE)?captureTimeDiffMs:0));
                                TimeSpan blackTimespan = new(0, 0, 0, 0, messageIn.RemoteBoard.Board.Clocks.BlackClock - ((messageIn.RemoteBoard.Board.Turn == TurnCode.BLACK)?captureTimeDiffMs:0));

                                string whiteClockString = $"{whiteTimespan.Hours}:{whiteTimespan.Minutes.ToString().PadLeft(2, '0')}:{whiteTimespan.Seconds.ToString().PadLeft(2, '0')}";
                                string blackClockString = $"{blackTimespan.Hours}:{blackTimespan.Minutes.ToString().PadLeft(2, '0')}:{blackTimespan.Seconds.ToString().PadLeft(2, '0')}";
                                int runWho = messageIn.RemoteBoard.Board.Turn == TurnCode.WHITE ? 1 : messageIn.RemoteBoard.Board.Turn == TurnCode.BLACK ? 2 : 0;

                                _appDataService.SetClocks(whiteClockString, blackClockString, runWho.ToString());
                                _dgtEbDllFacade.SetClock(whiteClockString, blackClockString, runWho);
                                _appDataService.IsWhiteOnBottom = messageIn.RemoteBoard.Board.IsWhiteOnBottom;

                                _appDataService.ChessDotComBoardFEN = messageIn.RemoteBoard.Board.FenString;

                                break;
                            default:
                                _appDataService.UserMessageArrived("INTERNAL", "ERROR: Unknown message type recieved on the API!");
                                _logger.LogError($"Unknown message type recieved on the API!");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _appDataService.UserMessageArrived("INTERNAL", $"ERROR: Unknown message recieved on the API :: {ex.Message}");
                        _logger.LogError($"Unknown message type recieved on the API :: {ex.Message}");
                    }
                }

                _appDataService.ResetChessDotComRemoteBoardState();
                _appDataService.UserMessageArrived("INTERNAL", "DGT Angel has Disconnected");
            }
            else
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }
    }
}
