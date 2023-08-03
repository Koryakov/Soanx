using Soanx.TelegramAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Soanx.TelegramAnalyzer {
    public class TelegramBotHelper {
        private string token;
        private readonly TelegramBotClient botClient;
        private ManualResetEventSlim ReadyToFinish;
        private string returnedText;
        public TelegramBotSettings BotSettings { get; private set; }

        public TelegramBotHelper(TelegramBotSettings botSettings) {
            this.BotSettings = botSettings;
            botClient = new TelegramBotClient(botSettings.Token);
        }

        public async Task<string> SendSmsCodeRequest(string requestText) {
            ReadyToFinish = new();
            var chatId = new Telegram.Bot.Types.ChatId(BotSettings.ChatId);
            await botClient.SendTextMessageAsync(chatId: chatId, text: @requestText);
            botClient.StartReceiving(HandleUpdateAsync, PollingErrorHandler, null);

            ReadyToFinish.Wait();
            return returnedText;
        }

        async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct) {
            returnedText = update.Message.Text;
            ReadyToFinish.Set();
        }

        Task PollingErrorHandler(ITelegramBotClient bot, Exception ex, CancellationToken ct) {
            throw new Exception("Telegram bot StartReceiving threw exception: ", ex);
            Console.WriteLine($"Exception while polling for updates: {ex}");
            return Task.CompletedTask;
        }
    }
}
