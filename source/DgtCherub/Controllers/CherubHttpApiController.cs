using DgtCherub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DgtCherub.Controllers
{
    [Route("api/")]
    [ApiController]
    public sealed class CherubHttpApiController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IAppDataService _appDataService;

        public CherubHttpApiController(ILogger<CherubHttpApiController> logger, IAppDataService appData)
        {
            _logger = logger;
            _appDataService = appData;
        }

        [HttpGet]
        [Route("{action}/{source}")]
        public object MessageUser(string source = "Unknown", string message = "Message Empty")
        {
            // curl -G  http://192.168.1.10:37964/api/MessageUser/CLI --data-urlencode 'message=Just wanted to say Hiya by GET'
            _appDataService.UserMessageArrived(source, message);

            return new { isSuccess = true };
        }

        [HttpGet]
        [Route("{action}")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Part of API")]
        public object AreYouThere()
        {
            // Test: curl -G  http://192.168.1.10:37964/api/AreYouThere
            return new { isSuccess = true };
        }
    }
}