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

        public DgtBoardController(ILogger<Form1> logger, IAppDataService appData)
        {
            _logger = logger;
            _appDataService = appData;
        }

        [HttpGet]
        [Route("{action}/{source}")]
        public object MessageUser(string source = "Unknown", string message = "Message Empty")
        {
            // Test: curl -G 'http://localhost:37964/api/DgtBoard/MessageUser/CLI/' --data-urlencode 'message=Just wanted to say hi by GET'
            _appDataService.UserMessageArrived(source, message);

            return new { isSuccess = true };
        }

        [HttpGet]
        [Route("{action}")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Part of API")]
        public object AreYouThere()
        {
            // Test: curl -G 'http://localhost:37964/api/DgtBoard/DgtAngelDisconnected/CLI/'
            return new { isSuccess = true };
        }
    }
}