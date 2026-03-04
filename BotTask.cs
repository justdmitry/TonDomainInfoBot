using System.Text;
using System.Text.Json;
using NetTelegramBotApi;
using NetTelegramBotApi.Types;
using RecurrentTasks;
using TonLibDotNet;

namespace TonDomainInfoBot
{
    public class BotTask(ILogger<BotTask> logger, ITonClient tonClient, ITelegramBot bot)
        : IRunnable
    {
        public static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            // Delete webhook (if any)
            await bot.DeleteWebhook(new(), cancellationToken);

            var me = await bot.GetMe(cancellationToken);
            logger.LogDebug("Connected as {Name} (@{Username})", me.FirstName, me.Username);

            long offset = 0;
            var maxCycles = 100;
            while (!cancellationToken.IsCancellationRequested && maxCycles > 0)
            {
                maxCycles--;

                var updates = await bot.GetUpdates(new() { Offset = offset }, cancellationToken);
                if (updates == null)
                {
                    continue;
                }

                foreach (var update in updates)
                {
                    offset = update.UpdateId + 1;
                    var msg = update.Message;
                    if (msg != null)
                    {
                        try
                        {
                            await ProcessMessage(msg);
                        }
                        catch (RequestFailedException ex)
                        {
                            logger.LogError(ex, "Ooops");
                        }
                    }
                }
            }
        }

        protected async Task ProcessMessage(Message msg)
        {
            if (string.IsNullOrEmpty(msg.Text))
            {
                await bot.SendMessage(new() { ChatId = msg.Chat.Id, Text = "Only text messages are supported" });
                return;
            }

            if (await ProcessStartMessage(msg.Text, msg.Chat.Id))
            {
                return;
            }

            if (TonRecipes.TelegramNumbers.TryNormalizeName(msg.Text, out var number))
            {
                await AnswerDomain(msg.Chat.Id, msg.Text, "+" + number, "anonymous number", (tc, d) => TonRecipes.TelegramNumbers.GetAllInfoByName(tc, number), info => info.Address);
            }
            else if (TonRecipes.TelegramUsernames.TryNormalizeName(msg.Text, out var username))
            {
                await AnswerDomain(msg.Chat.Id, msg.Text, username + ".t.me", "username", (tc, d) => TonRecipes.TelegramUsernames.GetAllInfoByName(tc, username), info => info.Address);
            }
            else if (TonRecipes.RootDns.TryNormalizeName(msg.Text, out var domainName))
            {
                await AnswerDomain(msg.Chat.Id, msg.Text, domainName + ".ton", "domain", (tc, d) => TonRecipes.RootDns.GetAllInfoByName(tc, domainName), info => info.Address);
            }
            else
            {
                await bot.SendMessage(new()
                {
                    ChatId = msg.Chat.Id,
                    Text = $"This does not look like domain name or anonymous number. I understand only `*.ton` and `*.t.me` domains, and `+888...` numbers.{Environment.NewLine}Try `foundation.ton` for example.",
                    ParseMode = ParseMode.Markdown
                });
            }

        }

        protected async Task<bool> ProcessStartMessage(string text, long chatId)
        {
            switch (text.ToLowerInvariant())
            {
                case "/start":
                case "/about":
                case "/help":
                    break;

                default:
                    return false;
            }

            var responseText = @"*TON Domain Info bot*

Send me `*.ton` domain name, or `*.t.me` username, or +888... anonymous number, and I'll respond with detailed info about that item (parsed from smartcontract internal data).

Only second-level domains are supported (e.g. `this.is.alice.ton` is not allowed).

Feel free to inspect my code [on GitHub](https://github.com/justdmitry/TonDomainInfoBot), add a star to [TonLib.Net repo](https://github.com/justdmitry/TonLib.NET), or even tip [my author](https://t.me/just_dmitry).
";
            await bot.SendMessage(new() { ChatId = chatId, Text = responseText, ParseMode = ParseMode.MarkdownV2, LinkPreviewOptions = new() { IsDisabled = true } });

            return true;
        }

        protected async Task AnswerDomain<TAnswer>(long chatId,
            string domain,
            string normalizedDomain,
            string typeName,
            Func<ITonClient, string, Task<TAnswer>> executor,
            Func<TAnswer, string> addressExtractor)
        {
            try
            {
                logger.LogInformation("Reading {Domain} / {NormalizedDomain}...", domain, normalizedDomain);

                await bot.SendChatAction(new() { ChatId = chatId, Action = ChatAction.Typing });


                await tonClient.InitIfNeeded();
                await tonClient.Sync();
                var info = await executor(tonClient, domain);

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

                await bot.SendMessage(new() { ChatId = chatId, Text = text, ParseMode = ParseMode.Markdown, LinkPreviewOptions = new() { IsDisabled = true } });
            }
            catch (ArgumentOutOfRangeException ex)
            {
                await bot.SendMessage(new() { ChatId = chatId, Text = $"{Emoji.UpsideDownFace} {ex.Message}" });
            }
            catch (TonClientException ex)
            {
                await bot.SendMessage(new() { ChatId = chatId, Text = $"{Emoji.CryingFace} Error from TonLib: {ex.Message}{Environment.NewLine}{Environment.NewLine}(Sometimes public nodes fail, feel free to retry)." });
            }
            catch (Exception ex)
            {
                await bot.SendMessage(new() { ChatId = chatId, Text = $"{Emoji.StopSign} {ex.Message}{Environment.NewLine}{Environment.NewLine}Please retry, if problem persists - contact @just_dmitry" });
            }
        }
    }
}
