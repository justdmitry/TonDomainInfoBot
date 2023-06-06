using NetTelegramBot.Framework;
using NetTelegramBotApi.Requests;
using NetTelegramBotApi.Types;

namespace TonDomainInfoBot.Telegram
{
    public class StartCommand : ICommandHandler
    {
        public const string CommandName1 = "start";
        public const string CommandName2 = "about";
        public const string CommandName3 = "help";

        public Task ExecuteAsync(ICommand command, User user, Chat chat, BotBase bot, Update update, IServiceProvider serviceProvider)
        {
            var text = @"*TON Domain Info bot*

Send me `*.ton` domain name, or `*.t.me` username, or +888... anonymous number, and I'll respond with detailed info about that item (parsed from smartcontract internal data).

Only second-level domains are supported (e.g. `this.is.alice.ton` is not allowed).

Feel free to inspect my code [on GitHub](https://github.com/justdmitry/TonDomainInfoBot), add a star to [TonLib.Net repo](https://github.com/justdmitry/TonLib.NET), or even tip [my author](https://t.me/just_dmitry).
";
            return bot.SendAsync(new SendMessage(chat.Id, text) { ParseMode = SendMessage.ParseModeEnum.Markdown, DisableWebPagePreview = true });
        }
    }
}