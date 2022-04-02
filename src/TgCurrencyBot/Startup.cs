using TgCurrencyBot.Models;
using TgCurrencyBot.Services;

namespace TgCurrencyBot;

public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        var appSettings = Configuration.GetSection("AppSettings");

        services.AddHostedService<CurrencyService>();
        services.AddMemoryCache();
        services.AddControllers();
        services.AddCors();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();
        app.UseCors("AllowAll");
        app.UseCors(cors =>
            cors.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
            .SetIsOriginAllowed(origin => true));
        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        Bot.GetBotClientAsync().Wait();
    }
}