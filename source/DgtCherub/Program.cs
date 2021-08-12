using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace DgtCherub
{
    static class Program
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
        static void Main()
        {
            //TODO: Checks for chrome and livechess and rabbit install
            if (Process.GetProcesses().Any(name => name.ProcessName.ToLowerInvariant().Contains("rabbitconnect")))
            {
                ShowCantStartDialog("The DGT RabbitConnect software is already running.  Please close it and retry.");
            }
            else
            {
                // Make sure we only have one instance running...
                Mutex mutex = new(true, @"Local\DgtCherub.exe", out bool isMutexCreated);

                if (isMutexCreated)
                {
                    // ...and if there is only one instance continue as normal
                    Application.SetHighDpiMode(HighDpiMode.SystemAware);
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);

                    // All unhandled exceptions are forced to the custom handler
                    Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

                    // Add the event handler for handling non-UI thread exceptions to the event.
                    AppDomain.CurrentDomain.UnhandledException += (sender,exception) => {
                        ShowErrorDialog($"Terminating on Fatal Error{Environment.NewLine}{exception.ExceptionObject?.ToString()}");
                        Application.Exit();
                    };

                    // Add the event handler for handling UI thread exceptions to the event.
                    Application.ThreadException += (sender, exception) => {
                        ShowErrorDialog($"Terminating on Fatal Error{Environment.NewLine}{exception.Exception.Message}{Environment.NewLine}{exception.Exception.StackTrace}");
                        Application.Exit();
                    };

                    var host = Host.CreateDefaultBuilder(Array.Empty<string>())
                       .ConfigureWebHostDefaults(webBuilder =>
                       {
                           webBuilder.UseStartup<Startup>()
                                     .UseUrls("http://localhost:37964");
                       }).Build();


                    //Start everything
                    _ = host.RunAsync();
                    Application.Run(host.Services.GetRequiredService<Form1>());
                }
                else
                {
                    ShowCantStartDialog("DGT Cherub is already running on this machine, you must close it first.");
                    mutex.Close();
                }
            }
        }
    }
}
