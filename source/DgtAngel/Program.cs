using System;
using System.Net.Http;
using System.Threading.Tasks;
using DgtAngel.Services;
using DgtAngelLib;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace DgtAngel
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            // workaround to use JavaScript fetch to bypass url validation
            // see: https://github.com/dotnet/runtime/issues/52836
            builder.Services.AddScoped<HttpClient>(sp => new JsHttpClient(sp) { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            
            builder.Services.AddSingleton<IAppData, AppData>();
            builder.Services.AddSingleton<IChessDotComHelpers,ChessDotComHelpers>();
            
            builder.Services.AddTransient<IScriptWrapper, ScriptWrapper>(sp => new ScriptWrapper(sp.GetService<IJSRuntime>()));
            builder.Services.AddTransient<IChessDotComWatcher, ChessDotComWatcher>(sp => new ChessDotComWatcher(sp.GetService<IScriptWrapper>(), sp.GetService<IAppData>(), sp.GetService<IChessDotComHelpers>()));            
            builder.Services.AddTransient<IDgtLiveChess, DgtLiveChess>(sp => new DgtLiveChess());

            builder.Services.AddBrowserExtensionServices(options =>
            {
                options.ProjectNamespace = typeof(Program).Namespace;
            });

            await builder.Build().RunAsync();
        }
    }
}
