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

            if (TonRecipes.TelegramNumbers.TryNormalizeName(text, out var number))
            {
                return AnswerDomain(message, text, "+" + number, "anonymous number", (tc, d) => TonRecipes.TelegramNumbers.GetAllInfoByName(tc, number), info => info.Address);
            }
            else if (TonRecipes.TelegramUsernames.TryNormalizeName(text, out var username))
            {
                return AnswerDomain(message, text, username + ".t.me", "username", (tc, d) => TonRecipes.TelegramUsernames.GetAllInfoByName(tc, username), info => info.Address);
            }
            else if (TonRecipes.RootDns.TryNormalizeName(text, out var domainName))
            {
                return AnswerDomain(message, text, domainName + ".ton", "domain", (tc, d) => TonRecipes.RootDns.GetAllInfoByName(tc, domainName), info => info.Address);
            }
            else
            {
                return SendAsync(new SendMessage(message.Chat.Id, $"This does not look like domain name or anonymous number. I understand only `*.ton` and `*.t.me` domains, and `+888...` numbers.{Environment.NewLine}Try `foundation.ton` for example.") { ParseMode = SendMessage.ParseModeEnum.Markdown });
            }
        }

        protected override Task OnUnknownCommandAsync(ICommand command, User user, Chat chat, Update update, IServiceProvider serviceProvider)
        {
            return SendAsync(new SendMessage(chat.Id, "I do not understand, sorry"));
        }

        protected async Task AnswerDomain<TAnswer>(Message message,
            string domain,
            string normalizedDomain,
            string typeName,
            Func<ITonClient, string, Task<TAnswer>> executor,
            Func<TAnswer, string> addressExtractor)
        {
            try
            {
                logger.LogInformation("Reading {Domain} / {NormalizedDomain}...", domain, normalizedDomain);

                await SendAsync(new SendChatAction(message.Chat.Id, "typing")).ConfigureAwait(false);

                await tonClient.InitIfNeeded().ConfigureAwait(false);
                var info = await executor(tonClient, domain).ConfigureAwait(false);

                // trim starting spaces in every line
                var json = JsonSerializer.Serialize(info, jsonOptions).Replace(Environment.NewLine + "  ", Environment.NewLine);

                // build message
                var text = string.Join(
                    Environment.NewLine,
                    $"Information for {typeName} [{normalizedDomain}](https://tonscan.org/address/{addressExtractor(info)}):",
                    string.Empty,
                    "```",
                    json,
                    "```");

                await SendAsync(new SendMessage(message.Chat.Id, text) { ParseMode = SendMessage.ParseModeEnum.Markdown, DisableWebPagePreview = true });
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
