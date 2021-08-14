using DgtEbDllWrapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DgtCherub
{
    [Route("[controller]")]
    [Controller]
    public class DgtVirtualClockController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IAppDataService _appDataService;

        public DgtVirtualClockController(ILogger<Form1> logger, IAppDataService appData)
        {
            _logger = logger;
            _appDataService = appData;
        }

#pragma warning disable CA1822 // DO NOT Mark members as static - it's part of the API!
        [HttpGet]
        [Route("{action}/{string1}/{string2}/{int1:int}")]
        public object TestResponse(string string1 = "none", string string2 = "none", int int1 = -1)

        {
            //  curl http://localhost:37964/DgtVirtualClock/TestResponse/st1a/st2a/3 ; echo
            return new { TestString1 = string1, TestString2 = string2, TestInt1 = int1, CalledAt = System.DateTime.Now };
        }
#pragma warning restore CA1822



        [HttpGet]
        [Route("{action}")]
        public ContentResult GetClock()
        {
            string htmlOut = System.IO.File.ReadAllText(@"C:/TESTHTML/DgtAngelClock.html");

            System.Console.WriteLine(">>>GET CLOCK");

            return new ContentResult
            {
                ContentType = "text/html",
                StatusCode = (int)HttpStatusCode.OK,
                Content = htmlOut,
                //Content = "<html><body>Hello World<script>var source=new EventSource('/DgtVirtualClock/GetStuff')</script></body></html>"
            };



            // http://localhost:37964/DgtVirtualClock/GetClock
        }

        [HttpGet]
        [Route("{action}/{fileName}")]
        public ActionResult Images(string fileName)
        {
            if (!string.IsNullOrWhiteSpace(fileName) && fileName == "blah")
            {
                byte[] imageData = System.IO.File.ReadAllBytes(@"C:/TESTHTML/test.jpg");
                string fileType = "image/jpeg";
                return File(imageData, fileType);
            }

            return StatusCode(404);
        }


        [HttpGet]
        [Route("{action}")]
        public async Task GetStuff()
        {
            string[] data = new string[] {  "Hello World!",
                                            "Hello Galaxy!",
                                            "Hello Universe!"
                                        };

            // http://localhost:37964/DgtVirtualClock/GetStuff

            Response.Headers.Add("Content-Type", "text/event-stream");


            _appDataService.OnLocalFenChange += async () =>
            {
                //Action updateAction = new(async () =>
                //{
                    string dataItem = $"data: {DateTime.Now.ToLongTimeString()} - {_appDataService.LocalBoardFEN}\n\n";
                    byte[] dataItemBytes = ASCIIEncoding.ASCII.GetBytes(dataItem);
                    await Response.Body.WriteAsync(dataItemBytes, 0, dataItemBytes.Length);
                    await Response.Body.FlushAsync();
                //});
            };

            //Keep Alive
            while (true)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    await Task.Delay(TimeSpan.FromSeconds(120));
                    string dataItem = $"data: {DateTime.Now.ToLongTimeString()} - {data[i]}\n\n";
                    byte[] dataItemBytes = ASCIIEncoding.ASCII.GetBytes(dataItem);
                    await Response.Body.WriteAsync(dataItemBytes, 0, dataItemBytes.Length);
                    await Response.Body.FlushAsync();
                }
            }
        }

    }
}
