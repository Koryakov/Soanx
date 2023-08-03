using Microsoft.Extensions.Configuration;
using Soanx.TelegramAnalyzer;
using Soanx.TelegramAnalyzer.Models;
using Soanx.TelegramModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Soanx.UnitTests {
    public class AuthorizingBotTest {

        private readonly ManualResetEventSlim ReadyToFinish = new();
        private TelegramBotSettings botSettings;

        public AuthorizingBotTest() {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json").AddEnvironmentVariables().Build();
            botSettings = config.GetRequiredSection("TelegramBotSettings").Get<TelegramBotSettings>();
        }

        [Fact]
        public async Task GetReceiveMessageWithBotTest() {
            var botClient = new TelegramBotClient(botSettings.Token);

            var chatId = new Telegram.Bot.Types.ChatId(botSettings.ChatId);
            await botClient.SendTextMessageAsync(chatId: chatId, text: $"Send me SMS code from phone");
            botClient.StartReceiving(HandleUpdateAsync, PollingErrorHandler, null);

            ReadyToFinish.Wait();
        }

        async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct) {
            string adminUserResponseMessageText = update.Message.Text;
            ReadyToFinish.Set();
        }

        Task PollingErrorHandler(ITelegramBotClient bot, Exception ex, CancellationToken ct) {
            throw new Exception("Telegram bot StartReceiving threw exception: ", ex);
            Console.WriteLine($"Exception while polling for updates: {ex}");
            return Task.CompletedTask;
        }

        [Fact]
        public async Task TgReadWorkerAuthorizationTest() {
            //var tgReadWorker = new TgReadWorker();
        }
    }
}
