using DgtCherub.Helpers;
using DgtCherub.Services;
using DgtRabbitWrapper.DgtEbDll;
using DgtLiveChessWrapper;
using DynamicBoard;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DgtRabbitWrapper;

namespace DgtCherub
{
    internal class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This method gets called by the runtime.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "This is a circle!")]
        public void ConfigureServices(IServiceCollection services)
        {
            _ = services.AddCors(options =>
            {
                options.AddPolicy("DevCorsPolicy", builder =>
                {
                    _ = builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            _ = services.AddHttpClient();
            _ = services.AddControllers();
            _ = services.AddLogging();
            _ = services.AddTransient(typeof(ISequentialVoicePlayer), typeof(SequentialVoicePlayer));
            _ = services.AddSingleton(typeof(IBoardRenderer), typeof(ShadowBoardRenderer));
            _ = services.AddSingleton(typeof(IAngelHubService), typeof(AngelHubService));
            _ = services.AddSingleton(typeof(IDgtEbDllFacade), typeof(DgtEbDllFacade));
            _ = services.AddSingleton(typeof(IDgtLiveChess), typeof(DgtLiveChess));
            _ = services.AddSingleton<Form1>();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This method gets called by the runtime.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "This method gets called by the runtime.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "Take it out and get a different warning!")]
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //_ = app.UseDeveloperExceptionPage()
            _ = app.UseRouting()
                   .UseWebSockets()
                   .UseCors("DevCorsPolicy")
                   .UseEndpoints(endpoints =>
                   {
                       _ = endpoints.MapControllers();
                   });
        }
    }
}
