using System.Text;
using System.Text.Json;
using NetTelegramBot.Framework;
using NetTelegramBotApi.Requests;
using NetTelegramBotApi.Types;
using TonLibDotNet;

namespace TonDomainInfoBot.Telegram
{
    public class Bot : BotBase
    {
        private readonly ITonClient tonClient;
        private readonly ILogger logger;

        private readonly JsonSerializerOptions jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public Bot(ILogger<Bot> logger, ICommandParser commandParser, IHttpClientFactory httpClientFactory, ITonClient tonClient)
            : base(logger, commandParser, httpClientFactory)
        {
            this.tonClient = tonClient;
            this.logger = logger;

            this.RegisteredCommandHandlers.Add(StartCommand.CommandName1, typeof(StartCommand));
            this.RegisteredCommandHandlers.Add(StartCommand.CommandName2, typeof(StartCommand));
            this.RegisteredCommandHandlers.Add(StartCommand.CommandName3, typeof(StartCommand));
        }

        protected override Task OnUnhandledCallbackQueryAsync(CallbackQuery callbackQuery, Update update, IServiceProvider serviceProvider)
        {
            return Task.CompletedTask;
        }

        protected override Task OnUnhandledChannelPostAsync(Message channelPost, Update update, IServiceProvider serviceProvider)
        {
            return Task.CompletedTask;
        }

        protected override Task OnUnhandledMessageAsync(Message message, Update update, IServiceProvider serviceProvider)
        {
            var text = message.Text.Trim();
            if (string.IsNullOrEmpty(text))
            {
                return SendAsync(new SendMessage(message.Chat.Id, "Only text messages are supported"));
            }

            if (text.IndexOf(' ') != -1)
            {
                return SendAsync(new SendMessage(message.Chat.Id, "Please, send only domain name (without other words, formatting etc.), for example `foundation.ton`.") { ParseMode = SendMessage.ParseModeEnum.Markdown });
            }

            text = text.TrimEnd('.').ToLowerInvariant();
            if (text.EndsWith(".ton"))
            {
                return AnswerDomain(message, text, d => TonRecipes.RootDns.GetNftIndex(d), (tc, d) => TonRecipes.RootDns.GetAllInfoByName(tc, d));
            }
            else if (text.EndsWith(".t.me"))
            {
                return AnswerDomain(message, text, d => TonRecipes.Telemint.GetNftIndex(d), (tc, d) => TonRecipes.Telemint.GetAllInfoByName(tc, d));
            }
            else
            {
                return SendAsync(new SendMessage(message.Chat.Id, $"This does not look like domain name. I understand only `*.ton` and `*.t.me` domains.{Environment.NewLine}Try `foundation.ton` for example.") { ParseMode = SendMessage.ParseModeEnum.Markdown });
            }
        }

        protected override Task OnUnknownCommandAsync(ICommand command, User user, Chat chat, Update update, IServiceProvider serviceProvider)
        {
            return SendAsync(new SendMessage(chat.Id, "I do not understand, sorry"));
        }

        protected async Task AnswerDomain<TAnswer>(Message message, string domain, Action<string> validator, Func<ITonClient, string, Task<TAnswer>> executor)
        {
            try
            {
                validator(domain);

                logger.LogInformation("Reading {Domain}...", domain);

                await SendAsync(new SendChatAction(message.Chat.Id, "typing")).ConfigureAwait(false);

                await tonClient.InitIfNeeded().ConfigureAwait(false);
                var info = await executor(tonClient, domain).ConfigureAwait(false);

                var json = JsonSerializer.Serialize(info, jsonOptions).Replace(Environment.NewLine + "  ", Environment.NewLine);

                await SendAsync(new SendMessage(message.Chat.Id, "```" + Environment.NewLine + json + Environment.NewLine + "```") { ParseMode = SendMessage.ParseModeEnum.Markdown });
            }
            catch (ArgumentOutOfRangeException ex)
            {
                await SendAsync(new SendMessage(message.Chat.Id, $"{Emoji.UpsideDownFace} {ex.Message}"));
            }
            catch (TonClientException ex)
            {
                await SendAsync(new SendMessage(message.Chat.Id, $"{Emoji.CryingFace} Error from TonLib: {ex.Message}{Environment.NewLine}{Environment.NewLine}Sometimes public nodes fail, feel free to retry."));
            }
            catch (Exception ex)
            {
                await SendAsync(new SendMessage(message.Chat.Id, $"{Emoji.StopSign} {ex.Message}{Environment.NewLine}{Environment.NewLine}Please retry, if problem persists - contact @just_dmitry"));
            }
        }
    }
}
