using DgtCherub.Helpers;
using DgtCherub.Services;
using DgtEbDllWrapper;
using DgtLiveChessWrapper;
using DynamicBoard;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Media;

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
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("DevCorsPolicy", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            services.AddHttpClient();
            services.AddControllers();
            services.AddLogging();
            services.AddScoped(typeof(SoundPlayer));
            services.AddTransient(typeof(IBoardRenderer), typeof(ShadowBoardRenderer));
            services.AddSingleton(typeof(ISequentialVoicePlayer), typeof(SequentialVoicePlayer));
            services.AddSingleton(typeof(IAngelHubService), typeof(AngelHubService));
            services.AddSingleton(typeof(IDgtEbDllFacade), typeof(DgtEbDllFacade));
            services.AddSingleton(typeof(IDgtLiveChess), typeof(DgtLiveChess));
            services.AddScoped<Form1>();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This method gets called by the runtime.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "This method gets called by the runtime.")]
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseDeveloperExceptionPage()
                .UseRouting()
                .UseWebSockets()
                .UseCors("DevCorsPolicy")
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });
        }
    }
}
