using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
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
    }
}
