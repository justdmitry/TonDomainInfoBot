using NetTelegramBotApi;
using TonLibDotNet;
using TonLibDotNet.Types;

namespace TonDomainInfoBot
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.Logging.AddSystemdConsole();

            ConfigureServices(builder.Services, builder.Configuration);

            var app = builder.Build();

            await app.RunAsync();
        }

        private static void ConfigureServices(IServiceCollection services, ConfigurationManager configuration)
        {
            var tempDir = configuration["TonClientTempDir"];

            services.Configure<TonOptions>(o =>
            {
                o.UseMainnet = true;
                o.ConfigPathMainnet = new("https://ton.org/global.config.json");
                o.Options.KeystoreType = new KeyStoreTypeDirectory(tempDir ?? ".");
            });

            services.AddSingleton<ITonClient, TonClient>();

            services.AddHttpClient<ITelegramBot, Bot>();

            services.AddTask<BotTask>(o => o.AutoStart(BotTask.Interval, TimeSpan.FromSeconds(3)));

            services.AddTask<ReconnectTask>(o => o.AutoStart(ReconnectTask.Interval));
        }
    }
}