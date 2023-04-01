using DgtCherub.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.NetworkInformation;
using System.Windows.Forms;

namespace DgtCherub
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            const string APP_GUID = "9fba0717-36d6-470b-a7c7-5e7b2491b91d"; //This is not a secret...Just used for the mutex
            const int CHERUB_API_LISTEN_PORT = 37964;
            const int CHERUB_GRPC_LISTEN_PORT = 37965;

            // Make sure we only have one instance running...
            using Mutex mutex = new(false, "Global\\" + APP_GUID);
            if (!mutex.WaitOne(0, false))
            {
                Dialogs.ShowCantStartDialog("Cherub is already running on this machine, you must close it first.");
                mutex.Close();
            }
            //...and that we can listen on the correct socket
            else if (IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Any(endpoint => endpoint.Port == CHERUB_API_LISTEN_PORT))
            {
                Dialogs.ShowCantStartDialog($"Another service is listening on port {CHERUB_API_LISTEN_PORT}.  Please stop it and retry.");
            }
            else
            {
                // ...and if there is only one instance continue as normal
                _ = Application.SetHighDpiMode(HighDpiMode.SystemAware);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // All unhandled exceptions are forced to the custom handler
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

                // Add an event handler for handling non-UI thread exceptions to the event.
                AppDomain.CurrentDomain.UnhandledException += (sender, exception) =>
                {
                    Dialogs.ShowErrorDialog($"Terminating on Fatal Error{Environment.NewLine}{exception.ExceptionObject}");
                    Environment.Exit(0); // Fatal error - using Env vs Application to quit immediately
                };

                // Add an event handler for handling UI thread exceptions to the event.
                Application.ThreadException += (sender, exception) =>
                {
                    Dialogs.ShowErrorDialog($"Terminating on Fatal Error{Environment.NewLine}{exception.Exception.Message}{Environment.NewLine}{exception.Exception.StackTrace}");
                    Environment.Exit(0); // Fatal error - using Env vs Application to quit immediately
                };

                //Set up DI
                IHost host = Host.CreateDefaultBuilder(Array.Empty<string>())
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.ConfigureKestrel(options =>
                        {
                            options.ListenAnyIP(CHERUB_API_LISTEN_PORT, listenOptions =>
                            {
                                listenOptions.Protocols = HttpProtocols.Http1;
                            });
                            options.ListenAnyIP(CHERUB_GRPC_LISTEN_PORT, listenOptions =>
                            {
                                listenOptions.Protocols = HttpProtocols.Http2;
                            });
                        });
                        _ = webBuilder.UseStartup<Startup>()
                                      .UseUrls($"http://0.0.0.0:{CHERUB_API_LISTEN_PORT}");
                    }).UseConsoleLifetime().Build();


                //Start everything
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(host.Services.GetRequiredService<Form1>());
            }
        }
    }
}

