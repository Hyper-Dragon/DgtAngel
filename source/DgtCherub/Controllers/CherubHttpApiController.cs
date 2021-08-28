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
        private readonly IAngelHubService _appDataService;

        public CherubHttpApiController(ILogger<CherubHttpApiController> logger, IAngelHubService appData)
        {
            _logger = logger;
            _appDataService = appData;
        }

        [HttpGet]
        [Route("{action}/{source}")]
        // curl -G  http://127.0.0.1:37964/api/MessageUser/CLI --data-urlencode 'message=Just wanted to say Hiya by GET'
        public object MessageUser(string source = "Unknown", string message = "Message Empty")
        {
            _logger?.LogDebug($"CherubHttpApiController MessageUser [{source}] [{message}]");
            _appDataService.UserMessageArrived(source, message);

            return new { isSuccess = true };
        }

        [HttpGet]
        [Route("{action}")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Part of API")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "This is a circle!")]
        // Test: curl -G  http://127.0.0.1:37964/api/AreYouThere
        public object AreYouThere()
        {
            _logger?.LogDebug($"CherubHttpApiController AreYouThere");
            return new { isSuccess = true };
        }
    }
}