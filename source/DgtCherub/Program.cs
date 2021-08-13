using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Windows.Forms;

namespace DgtCherub
{
    internal static class Program
    {
        private static void ShowCantStartDialog(string message)
        {
            MessageBox.Show($"{message}",
                             "Dgt Cherub - Statup Failed",
                             MessageBoxButtons.OK,
                             MessageBoxIcon.Warning,
                             MessageBoxDefaultButton.Button1);
        }

        private static void ShowErrorDialog(string message)
        {
            MessageBox.Show($"{message}",
                             "Dgt Cherub - Fatal Error",
                             MessageBoxButtons.OK,
                             MessageBoxIcon.Error,
                             MessageBoxDefaultButton.Button1);
        }

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            const string APP_GUID = "9fba0717-36d6-470b-a7c7-5e7b2491b91d"; //This is not a secret...Just used for the mutex
            const int CHERUB_API_LISTEN_PORT = 37964;

            // Make sure we only have one instance running...
            using Mutex mutex = new(false, "Global\\" + APP_GUID);
            if (!mutex.WaitOne(0, false))
            {
                ShowCantStartDialog("DGT Cherub is already running on this machine, you must close it first.");
                mutex.Close();
            }
            //...and that we can listen on the correct socket
            else if (IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Any(endpoint => endpoint.Port == CHERUB_API_LISTEN_PORT) )
            {
                ShowCantStartDialog($"Another service is listening on port {CHERUB_API_LISTEN_PORT}.  Please stop it and retry.");
            }
            else
            {
                // ...and if there is only one instance continue as normal
                Application.SetHighDpiMode(HighDpiMode.SystemAware);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // All unhandled exceptions are forced to the custom handler
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

                // Add an event handler for handling non-UI thread exceptions to the event.
                AppDomain.CurrentDomain.UnhandledException += (sender, exception) =>
                {
                    ShowErrorDialog($"Terminating on Fatal Error{Environment.NewLine}{exception.ExceptionObject}");
                    Environment.Exit(0); // Fatal error - using Env vs Application to quit immediately
                };

                // Add an event handler for handling UI thread exceptions to the event.
                Application.ThreadException += (sender, exception) =>
                {
                    ShowErrorDialog($"Terminating on Fatal Error{Environment.NewLine}{exception.Exception.Message}{Environment.NewLine}{exception.Exception.StackTrace}");
                    Environment.Exit(0); // Fatal error - using Env vs Application to quit immediately
                };

                //Set up DI
                IHost host = Host.CreateDefaultBuilder(Array.Empty<string>())
                   .ConfigureWebHostDefaults(webBuilder =>
                   {
                       webBuilder.UseStartup<Startup>()
                                 .UseUrls($"http://localhost:{CHERUB_API_LISTEN_PORT}");
                   }).Build();


                //Start everything
                _ = host.RunAsync();
                Application.Run(host.Services.GetRequiredService<Form1>());
            }
        }
    }
}

