using DgtEbDllWrapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DgtCherub
{
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

#pragma warning disable CA1822 // DO NOT Mark members as static - it's part of the API!
        [HttpGet]
        [Route("{action}/{string1}/{string2}/{int1:int}")] 
        public object TestResponse(string string1 = "none", string string2 = "none", int int1 = -1)
 
        {
            //  curl http://localhost:37964/api/DgtBoard/TestResponse/st1a/st2a/3 ; echo
            return new { TestString1 = string1, TestString2 = string2, TestInt1 = int1, CalledAt = System.DateTime.Now };
        }
#pragma warning restore CA1822

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
        public object MessageUser(string source= "Unknown", string message = "Message Empty")
        {
            // curl -G 'http://localhost:37964/api/DgtBoard/MessageUser/CLI/' --data-urlencode 'message=Just wanted to say hi by GET'
            _appDataService.UserMessageArrived(source, message);

            return new { isSuccess=true };
        }

        [HttpGet]
        [Route("{action}/{source}")]
        public object SetLocalBoardFenString(string source = "Unknown", string fen = "8/8/8/8/8/8/8/8")
        {
            // curl -G 'http://localhost:37964/api/DgtBoard/SetLocalBoardFenString/CLI/' --data-urlencode 'fen=8/5k2/8/3B4/8/8/7K/8'
            
            if(_appDataService.LocalBoardFEN == fen)
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
        public object SetChessDotComFenString(string source = "Unknown", string fen = "8/8/8/8/8/8/8/8")
        {
            // curl -G 'http://localhost:37964/api/DgtBoard/SetChessDotComFenString/CLI/' --data-urlencode 'fen=rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR'

            if (_appDataService.ChessDotComBoardFEN == fen)
            {
                _appDataService.UserMessageArrived(source, $"Chess.com board FEN is a DUPLICATE");
            }
            else
            {
                _appDataService.ChessDotComBoardFEN = fen;
                _appDataService.UserMessageArrived(source, $"Chess.com board FEN is {fen}");
            }

            return new { isSuccess = true };
        }

    }
}
