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

        // This method gets called by the runtime. Use this method to add services to the container.
#pragma warning disable CA1822 // DO NOT Mark members as static
        public void ConfigureServices(IServiceCollection services)
#pragma warning restore CA1822 
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
            services.AddTransient(typeof(IBoardRenderer), typeof(ChessDotComBoardRenderer));
            services.AddSingleton(typeof(IAppDataService), typeof(AppDataService));
            services.AddSingleton(typeof(IDgtEbDllFacade), typeof(DgtEbDllFacade));
            services.AddSingleton(typeof(IDgtLiveChess), typeof(DgtLiveChess));
            services.AddScoped(typeof(SoundPlayer));
            services.AddScoped<Form1>();
        }


#pragma warning disable CA1822  // DO NOT Mark members as static OR  Remove unused parameter
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
#pragma warning restore CA1822 
        {
            app.UseDeveloperExceptionPage()
                .UseRouting()
                .UseCors("DevCorsPolicy")
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });
        }
    }
}
