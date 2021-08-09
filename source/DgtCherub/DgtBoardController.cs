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

        [HttpGet]
        [Route("{action}/{string1}/{string2}/{int1:int}")]
        public object TestResponse(string string1 = "none", string string2 = "none", int int1 = -1)
        {
            //  curl http://localhost:37964/api/DgtBoard/TestResponse/st1a/st2a/3 ; echo
            return new { TestString1 = string1, TestString2 = string2, TestInt1 = int1, CalledAt = System.DateTime.Now };
        }

        [HttpGet]
        [Route("{action}/{whiteClock}/{blackClock}/{runwho:int}")]
        public object SetClock(string whiteClock = "0:00:00", string blackClock = "0:00:00", int runwho = 0)
        {
            //curl http://localhost:37964/api/DgtBoard/SetClock/0:30:00/0:30:00/1
            _appDataService.SetClocks(whiteClock,blackClock);
            _dgtEbDllFacade.SetClock(whiteClock,blackClock,runwho);

            return new { White = whiteClock, Black = blackClock, Time = System.DateTime.Now };
        }

    }
}
