using System;
using System.Net.Http;
using System.Threading.Tasks;
using DgtAngel.Services;
using DgtAngelLib;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace DgtAngel
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.Logging.SetMinimumLevel(LogLevel.None);
            builder.RootComponents.Add<App>("#app");

            // workaround to use JavaScript fetch to bypass url validation
            // see: https://github.com/dotnet/runtime/issues/52836
            builder.Services.AddScoped<HttpClient>(sp => new JsHttpClient(sp) { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) })
                            .AddSingleton<IAppData,AppData>()
                            .AddSingleton<IChessDotComHelpers,ChessDotComHelpers>()
                            .AddTransient<IScriptWrapper, ScriptWrapper>(sp => new ScriptWrapper(sp.GetService<IJSRuntime>()))
                            .AddTransient<IChessDotComWatcher, ChessDotComWatcher>(sp => new ChessDotComWatcher(sp.GetService<IScriptWrapper>(),  
                                                                                   sp.GetService<IChessDotComHelpers>()))
                            .AddTransient<IDgtLiveChess, DgtLiveChess>(sp => new DgtLiveChess())
                            .AddBrowserExtensionServices(options =>{ options.ProjectNamespace = typeof(Program).Namespace; });

            await builder.Build().RunAsync();
        }
    }
}
