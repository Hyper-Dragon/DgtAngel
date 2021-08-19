using DgtAngel.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace DgtAngel
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.Logging.SetMinimumLevel(LogLevel.Trace); //TODO: Fix this -> level does not work!
            builder.RootComponents.Add<App>("#app");

            // workaround to use JavaScript fetch to bypass url validation
            // see: https://github.com/dotnet/runtime/issues/52836
            builder.Services.AddScoped<HttpClient>(sp => new JsHttpClient(sp) { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) })
                            .AddSingleton<IScriptWrapper, ScriptWrapper>()
                            .AddSingleton<IChessDotComWatcher, ChessDotComWatcher>()
                            .AddSingleton<ICherubConnectionManager, CherubConnectionManager>()
                            .AddBrowserExtensionServices(options => { options.ProjectNamespace = typeof(Program).Namespace; });

            await builder.Build().RunAsync();
        }
    }
}
