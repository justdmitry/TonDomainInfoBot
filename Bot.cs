using NetTelegramBotApi;

namespace TonDomainInfoBot
{
    public class Bot(IConfiguration configuration, HttpClient httpClient)
        : TelegramBot(configuration["TelegramBotToken"], httpClient)
    {
        // Nothing.
    }
}
