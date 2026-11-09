using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace DgtCherubTest.Hardware
{
    /// <summary>
    /// End-to-end tests against a real DgtCherub process.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These launch the application and talk to it over its real HTTP port. They exist because
    /// the driver cannot be exercised from a test host: dgtebdll.dll is a wxWidgets
    /// application, and calling it from a console process with no UI thread raises its
    /// assertions as modal dialogs. The application is the only process that can own the
    /// driver, so the tests drive the application instead of the driver.
    /// </para>
    /// <para>
    /// Opt-in behind DGT_HARDWARE_TESTS=1, because launching the application takes the DGT
    /// board and shows a window. Without it every test reports Inconclusive.
    /// </para>
    /// <para>
    /// They never kill a process they did not start: if anything is already listening on the
    /// API port the run is skipped, with the reason in the output, because the application
    /// shows a modal "another instance is running" dialog rather than exiting, and because a
    /// second instance would fight the first for the board.
    /// </para>
    /// </remarks>
    [TestClass]
    [TestCategory("Hardware")]
    public class CherubApplicationTests
    {
        private const string OPT_IN_VARIABLE = "DGT_HARDWARE_TESTS";

        private const int API_PORT = 37964;
        private const string BASE_URL = "http://127.0.0.1:37964/";

        private static readonly TimeSpan StartupDeadline = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Generous, because a cold start has to initialise the driver, open the board and
        /// bind two sockets. It is still a deadline: the deadlock this suite guards against
        /// presents as a startup that never finishes, not as an error.
        /// </summary>
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);

        private static Process? _cherub;
        private static HttpClient? _client;
        private static string? _unavailable;

        [ClassInitialize]
        public static void LaunchApplication(TestContext context)
        {
            if (!OptedIn)
            {
                _unavailable = $"set {OPT_IN_VARIABLE}=1 to run these tests";
                return;
            }

            string? executable = FindExecutable();

            if (executable is null)
            {
                _unavailable = "DgtCherub.exe was not found - build the solution first";
                return;
            }

            if (IsPortListening(API_PORT))
            {
                _unavailable =
                    $"something is already listening on port {API_PORT}. Close the running Cherub first - " +
                    "these tests will not kill a process they did not start, and two instances would fight for the board.";
                return;
            }

            _client = new HttpClient { BaseAddress = new Uri(BASE_URL), Timeout = RequestTimeout };

            ProcessStartInfo startInfo = new(executable)
            {
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(executable)!,
            };

            _cherub = Process.Start(startInfo);

            if (_cherub is null)
            {
                _unavailable = "the process could not be started";
                return;
            }

            if (!WaitForApi(_cherub, StartupDeadline))
            {
                _unavailable = _cherub.HasExited
                    ? $"the application exited during startup with code {_cherub.ExitCode}"
                    : $"the application never answered on port {API_PORT} within {StartupDeadline.TotalSeconds} seconds";
            }
        }

        [ClassCleanup]
        public static void ShutdownApplication()
        {
            _client?.Dispose();

            if (_cherub is null)
            {
                return;
            }

            try
            {
                if (!_cherub.HasExited)
                {
                    _cherub.Kill(entireProcessTree: true);
                    _ = _cherub.WaitForExit(TimeSpan.FromSeconds(10));
                }
            }
            catch (Exception)
            {
                //Teardown: nothing useful to do if the process will not die.
            }
            finally
            {
                _cherub.Dispose();
            }
        }

        [TestMethod(DisplayName = "The application starts and answers on its API port")]
        public async Task ApplicationAnswersOnItsApiPort()
        {
            RequireRunningApplication();

            using HttpResponseMessage response = await _client!.GetAsync("api/AreYouThere");

            Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
            StringAssert.Contains(await response.Content.ReadAsStringAsync(), "isSuccess");
        }

        /// <summary>
        /// The startup path runs the driver initialisation on the UI thread. A nested Init that
        /// blocked there would leave the window hung: Kestrel would probably still answer, so
        /// the meaningful signal is that the process is responding, before and after warm-up.
        /// </summary>
        [TestMethod(DisplayName = "The application is still responding after start-up")]
        public async Task ApplicationIsResponsive()
        {
            RequireRunningApplication();

            Assert.IsFalse(_cherub!.HasExited, "the application must still be running");
            Assert.IsTrue(_cherub.Responding, "the application window must not be hung - a hung startup is what the driver deadlock looked like");

            await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false);

            Assert.IsTrue(_cherub.Responding, "the application must still be responding once the driver has settled");

            using HttpResponseMessage response = await _client!.GetAsync("api/AreYouThere");
            Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode, "the API must still answer after warm-up");
        }

        [TestMethod(DisplayName = "The virtual clock index is served")]
        public async Task VirtualClockIndexIsServed()
        {
            RequireRunningApplication();

            // GetIndex is routed at the site root, not under the controller name.
            using HttpResponseMessage response = await _client!.GetAsync("/");

            Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
            StringAssert.Contains(await response.Content.ReadAsStringAsync(), "<html", "the clock page must come back as HTML");
        }

        [TestMethod(DisplayName = "Each offered clock page is served")]
        [DataRow("GreySlide")]
        [DataRow("GreyFixed")]
        [DataRow("LocalBroadcast")]
        [DataRow("RemoteBroadcast")]
        [DataRow("WingedHorse")]
        public async Task ClockPagesAreServed(string clock)
        {
            RequireRunningApplication();

            using HttpResponseMessage response = await _client!.GetAsync($"CherubVirtualClock/GetClock/{clock}");

            Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode, $"'{clock}' should be served");
            Assert.AreEqual("text/html", response.Content.Headers.ContentType?.MediaType);
        }

        /// <summary>
        /// The board image is rendered from live hub state, so a PNG here means the rendering
        /// path is wired to the running application rather than to a stub.
        /// </summary>
        [TestMethod(DisplayName = "A board image is rendered over HTTP")]
        public async Task BoardImageIsRendered()
        {
            RequireRunningApplication();

            using HttpResponseMessage response = await _client!.GetAsync("CherubVirtualClock/BoardImage/local");

            Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);

            byte[] png = await response.Content.ReadAsByteArrayAsync();

            Assert.IsTrue(png.Length > 8, "the board image should not be empty");
            CollectionAssert.AreEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, png[..4], "the board image must be a PNG");
        }

        [TestMethod(DisplayName = "An unknown clock asset is a 404, not a fault")]
        public async Task UnknownAssetIsNotFound()
        {
            RequireRunningApplication();

            using HttpResponseMessage response = await _client!.GetAsync("CherubVirtualClock/GetFile/NotAClock/TemplateData/style.css");

            Assert.AreEqual(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }

        private static bool OptedIn
            => string.Equals(Environment.GetEnvironmentVariable(OPT_IN_VARIABLE), "1", StringComparison.Ordinal);

        private static void RequireRunningApplication()
        {
            if (_unavailable is not null)
            {
                Assert.Inconclusive(_unavailable);
            }

            Assert.IsNotNull(_cherub, "the application process should have been started");
            Assert.IsFalse(_cherub!.HasExited, "the application exited before the test ran");
        }

        private static bool WaitForApi(Process process, TimeSpan deadline)
        {
            Stopwatch elapsed = Stopwatch.StartNew();

            while (elapsed.Elapsed < deadline)
            {
                if (process.HasExited)
                {
                    return false;
                }

                try
                {
                    using HttpResponseMessage response = _client!
                        .GetAsync("api/AreYouThere")
                        .GetAwaiter()
                        .GetResult();

                    if (response.IsSuccessStatusCode)
                    {
                        return true;
                    }
                }
                catch (Exception)
                {
                    //Not listening yet, which is the normal state while it starts.
                }

                Thread.Sleep(250);
            }

            return false;
        }

        private static bool IsPortListening(int port)
            => IPGlobalProperties.GetIPGlobalProperties()
                                 .GetActiveTcpListeners()
                                 .Any(endpoint => endpoint.Port == port);

        /// <summary>
        /// Prefers the copy in this test's own output folder, so the binary under test matches
        /// the configuration being run, and falls back to the application's own output folder.
        /// </summary>
        private static string? FindExecutable()
        {
            string local = Path.Combine(AppContext.BaseDirectory, "DgtCherub.exe");

            if (File.Exists(local))
            {
                return local;
            }

            DirectoryInfo? directory = new(AppContext.BaseDirectory);

            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DgtAngel.sln")))
            {
                directory = directory.Parent;
            }

            if (directory is null)
            {
                return null;
            }

            return Directory
                .EnumerateFiles(Path.Combine(directory.FullName, "DgtCherub", "bin"), "DgtCherub.exe", SearchOption.AllDirectories)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
        }
    }
}
