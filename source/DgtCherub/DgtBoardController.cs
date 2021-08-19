using DgtAngelShared.Json;
using DgtCherub;
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

namespace DgtCherub
{
    public class WebSocketController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IAppDataService _appDataService;

        public WebSocketController(ILogger<Form1> logger, IAppDataService appData)
        {
            _logger = logger;
            _appDataService = appData;
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
                    var buffer = new ArraySegment<byte>(new byte[1024]);
                    WebSocketReceiveResult result = null;
                    var allBytes = new List<byte>();

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
                        var messageIn = JsonSerializer.Deserialize<CherubApiMessage>(Encoding.UTF8.GetString(allBytes.ToArray(), 0, allBytes.Count));

                        switch (messageIn.MessageType)
                        {
                            case CherubApiMessage.MessageTypeCode.KEEP_ALIVE:
                                _logger.LogTrace($"Cherub Keep-Alive from {messageIn.Source}");
                                break;
                            case CherubApiMessage.MessageTypeCode.MESSAGE:
                                _appDataService.UserMessageArrived(messageIn.Source, messageIn.Message);
                                break;
                            case CherubApiMessage.MessageTypeCode.FEN_UPDATE:
                                _appDataService.UserMessageArrived(messageIn.Source, messageIn.RemoteBoard.Board.FenString);


















                                break;
                            default:
                                _appDataService.UserMessageArrived("INTERNAL", "ERROR: Unknown message type recieved on the API!");
                                _logger.LogError($"Unknown message type recieved on the API!");
                                break;
                        }
                    }
                    catch (Exception ex) {
                        _appDataService.UserMessageArrived("INTERNAL", $"ERROR: Unknown message recieved on the API :: {ex.Message}");
                        _logger.LogError($"Unknown message type recieved on the API :: {ex.Message}");
                    }
                }

                _appDataService.UserMessageArrived("INTERNAL", "DGT Angel Disconnected");
            }
            else
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }
    }
}


[Route("api/[controller]")]
[ApiController]
public class DgtBoardController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IAppDataService _appDataService;
    private readonly IDgtEbDllFacade _dgtEbDllFacade;

    public DgtBoardController(ILogger<Form1> logger, IAppDataService appData, IDgtEbDllFacade dgtEbDllFacade)
    {
        _logger = logger;
        _appDataService = appData;
        _dgtEbDllFacade = dgtEbDllFacade;
    }

    [HttpGet]
    [Route("{action}/{whiteClock}/{blackClock}/{runwho:int}")]
    public object SetClock(string whiteClock = "0:00:00", string blackClock = "0:00:00", int runwho = 0)
    {
        //curl http://localhost:37964/api/DgtBoard/SetClock/0:30:00/0:30:00/1
        _appDataService.SetClocks(whiteClock, blackClock, runwho.ToString());
        _dgtEbDllFacade.SetClock(whiteClock, blackClock, runwho);

        return new { White = whiteClock, Black = blackClock, Time = System.DateTime.Now };
    }

    [HttpGet]
    [Route("{action}/{source}")]
    public object MessageUser(string source = "Unknown", string message = "Message Empty")
    {
        // curl -G 'http://localhost:37964/api/DgtBoard/MessageUser/CLI/' --data-urlencode 'message=Just wanted to say hi by GET'
        _appDataService.UserMessageArrived(source, message);

        return new { isSuccess = true };
    }

    [HttpGet]
    [Route("{action}/{source}")]
    public object SetLocalBoardFenString(string source = "Unknown", string fen = "8/8/8/8/8/8/8/8")
    {
        // curl -G 'http://localhost:37964/api/DgtBoard/SetLocalBoardFenString/CLI/' --data-urlencode 'fen=8/5k2/8/3B4/8/8/7K/8'

        if (_appDataService.LocalBoardFEN == fen)
        {
            _appDataService.UserMessageArrived(source, $"Local DGT board FEN is a DUPLICATE");
        }
        else
        {
            _appDataService.UserMessageArrived(source, $"Local DGT board FEN is {fen}");
            _appDataService.LocalBoardFEN = fen;
        }

        return new { isSuccess = true };
    }

    [HttpGet]
    [Route("{action}/{source}")]
    public object SetChessDotComFenString(string source = "Unknown", string fen = "8/8/8/8/8/8/8/8", string isWhiteBottom = "true")
    {
        // curl -G 'http://localhost:37964/api/DgtBoard/SetChessDotComFenString/CLI/' --data-urlencode 'fen=rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR'

        //if ( string.IsNullOrWhiteSpace(fen) )
        //{
        //    _appDataService.UserMessageArrived(source, $"WARNING: Chess.com board FEN is BLANK");
        //}
        //else 

        if (_appDataService.ChessDotComBoardFEN == fen)
        {
            _appDataService.UserMessageArrived(source, $"Chess.com board FEN is a DUPLICATE");
        }
        else
        {
            _appDataService.IsWhiteOnBottom = bool.Parse(isWhiteBottom);
            _appDataService.ChessDotComBoardFEN = fen;
            _appDataService.UserMessageArrived(source, $"Chess.com board FEN is {fen}.");
        }

        return new { isSuccess = true };
    }

    [HttpGet]
    [Route("{action}/{source}")]
    public object DgtAngelDisconnected(string source = "Unknown")
    {
        // curl -G 'http://localhost:37964/api/DgtBoard/DgtAngelDisconnected/CLI/'

        _appDataService.ResetChessDotComRemoteBoardState();
        _appDataService.UserMessageArrived(source, $"Chess.com board disconnected.");

        return new { isSuccess = true };
    }

}

