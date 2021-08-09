using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Windows.Forms;

namespace DgtCherub
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Make sure we only have one instance running...
            Mutex mutex = new(true, @"Local\DgtCherub.exe", out bool isMutexCreated);

            if (isMutexCreated)
            {
                // ...and if there is only one instance continue as normal
                Application.SetHighDpiMode(HighDpiMode.SystemAware);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                var host = Host.CreateDefaultBuilder(Array.Empty<string>())
                   .ConfigureWebHostDefaults(webBuilder => {
                       webBuilder.UseStartup<Startup>()
                                 .UseUrls("http://localhost:37964");
                   }).Build();


                //Start everything
                //_ = DgtEbWrapper.Init();
                _ = host.RunAsync();
                Application.Run(host.Services.GetRequiredService<Form1>());
            }
            else
            {
                if (MessageBox.Show("DGT Cherub is already running on this machine, you must close it first.",
                                    "Dgt Cherub",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Exclamation,
                                    MessageBoxDefaultButton.Button1) == DialogResult.OK)
                {
                    mutex.Close();
                }
            }
        }
    }
}
