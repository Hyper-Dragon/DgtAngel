using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DgtCherub
{
    [Route("api/[controller]")]
    [ApiController]
    public class DgtBoardController : ControllerBase
    {
        private readonly ILogger<DgtBoardController> _logger;
        //private readonly IAppDataService _appData;

        public DgtBoardController(ILogger<DgtBoardController> logger)//, IAppDataService appData)
        {
            _logger = logger;
            //_appData = appData;
        }

        [HttpGet]
        [Route("{action}/{string1}/{string2}/{int1:int}")]
        public object TestResponse(string string1 = "none", string string2 = "none", int int1 = -1)
        {
            // http://localhost:37964/ClockApi/SetAuthors/3
            return new { TestString1 = string1, TestString2 = string2, TestInt1 = int1, CalledAt = System.DateTime.Now };
        }

    }
}
