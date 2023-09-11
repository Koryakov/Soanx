using static TdLib.TdApi.Update;
using static TdLib.TdApi;
using TdLib.Bindings;
using TdLib;
using Soanx.TelegramModels;
using Soanx.Repositories.Models;
using System.Text.Json;
using System.Threading.Tasks;
using Soanx.Repositories;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Soanx.TgWorker;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Collections.Concurrent;
using Soanx.TelegramAnalyzer.Models;
using Serilog;
using Serilog.Context;

namespace Soanx.TelegramAnalyzer;
public class TgMessageGrabbingWorker: ITelegramWorker {

    private object addingLockObj = new object();
    private Serilog.ILogger log = Log.ForContext<TgMessageGrabbingWorker>();
    public TdClient TdClient { get; private set; }
    public TgMessageGrabbingSettings TgGrabbingSettings { get; private set; }
    public List<TgGrabbingChat> TgGrabbingChats { get; private set; }
    public ConcurrentBag<TgMessage> CollectionForStoring { get; private set; }

    public TgMessageGrabbingWorker(TdClient tdClient, ConcurrentBag<TgMessage> collectionForStoring,
        TgMessageGrabbingSettings tgGrabbingSettings, List<TgGrabbingChat> tgGrabbingChats) {

        TdClient = tdClient;
        CollectionForStoring = collectionForStoring;
        TgGrabbingSettings = tgGrabbingSettings;
        TgGrabbingChats = tgGrabbingChats;

    }

    public async Task Run(CancellationToken cancellationToken) {
        log.Information("IN Run(). Grabbing settings: {@TgMessageGrabbingSettings}");

        List<Task> tasks = new List<Task>();
        foreach(var chatSetting in TgGrabbingChats) {
            tasks.Add(Task.Factory.StartNew(async () => 
                ReadTdMessagesIntoCollection(chatSetting, cancellationToken)));
        }

        await Task.WhenAll(tasks);
        log.Information("OUT Run()");
    }

    private async Task ReadTdMessagesIntoCollection(TgGrabbingChat grabbingChat, CancellationToken cancellationToken) {

        var locLog = log.ForContext("method", "ReadTdMessagesIntoCollection()").ForContext("chatId", grabbingChat.ChatId);
        locLog.Information("IN");

        int minUnixDate = DateTimeHelper.ToUnixTime(DateTime.UtcNow);
        var unixFromDate = DateTimeHelper.ToUnixTime(grabbingChat.ReadTillDate);
        long oldestMessageId = 0L;
        
        while (minUnixDate >= unixFromDate) {
            locLog.Information("TdClient.GetChatHistoryAsync() started... MinDate={@minDate}, fromMessageId={oldestMessageId}"
                , DateTimeHelper.FromUnixTime(minUnixDate), oldestMessageId);
            
            Messages msgBundle = await TdClient.GetChatHistoryAsync(
                chatId: grabbingChat.ChatId,
                fromMessageId: oldestMessageId,
                limit: TgGrabbingSettings.ChatHistoryReadingCount,
                onlyLocal: false);

            Message? oldestMessage = msgBundle.Messages_.OrderBy(m => m.Date).FirstOrDefault();
            if (oldestMessage != null && minUnixDate > oldestMessage.Date) {
                minUnixDate = oldestMessage.Date;
                oldestMessageId = oldestMessage.Id;
            }
            int readMessagesCount = msgBundle.Messages_.Count();
            locLog.Information("TdClient.GetChatHistoryAsync() finished, readMessagesCount count = {readMessagesCount}", readMessagesCount);

            int addingCounter = 0;
            lock (addingLockObj) {
                for (int i = 0; i < readMessagesCount; i++) {
                    var tdMsg = msgBundle.Messages_[i];

                    if (!CollectionForStoring.Any(adding =>
                        adding.TgChatId == tdMsg.ChatId && adding.TgMessageId == tdMsg.Id)) {
                        
                        var tgMessageRaw = MessageConverter.ConvertToTgMessage(tdMsg, SoanxTdUpdateType.None);
                        CollectionForStoring.Add(tgMessageRaw);
                        addingCounter++;
                    } else {
                        locLog.Verbose<Message>("Msg not added into collection: {@tdMsg}", tdMsg);
                    }
                }
            }
            locLog.Information("Filtered messages count = {addingCounter}", addingCounter);

            if (cancellationToken.IsCancellationRequested) {
                locLog.Information("OUT IsCancellationRequested.");
                return;
            }
            await Task.Delay(TgGrabbingSettings.ReadingMessagesInterval);
        }
        locLog.Information("OUT");
    }
}