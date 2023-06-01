using TonLibDotNet;
using TonLibDotNet.Types;

namespace TonDomainInfoBot
{
    public class Startup
    {
        private readonly IConfiguration configuration;

        public Startup(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTelegramBot<Telegram.Bot>(configuration.GetSection("TelegramOptions"));
            services.AddTelegramBotCommandHandlers(typeof(Startup).Assembly);

            var tempDir = configuration["TonClientTempDir"];

            services.Configure<TonOptions>(o =>
            {
                o.UseMainnet = true;
                o.Options.KeystoreType = new KeyStoreTypeDirectory(tempDir);
            });

            services.AddSingleton<ITonClient, TonClient>();
        }
        public void Configure(IApplicationBuilder app)
        {
            app.UseForwardedHeaders(new ForwardedHeadersOptions() { ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.All });

            app.UseStatusCodePages();
            app.UseMiddleware<RobotsTxtMiddleware>();

            app.UseTelegramBot<Telegram.Bot>();
        }
    }
}
