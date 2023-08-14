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
    //private readonly ConcurrentBag<TgMessageRaw> tgRawMessagesForStoring = new();

    private TdLibParametersModel tdLibParameters;
    private AppSettingsHelper appSettings = new();
    private object addingLockObj = new object();

    protected virtual TelegramRepository tgRepository { get; set; }

    public ITdClientAuthorizer TdClientAuthorizer { get; private set; }
    public TdClient TdClient { get; private set; }
    private Serilog.ILogger log = Log.ForContext<TgMessageGrabbingWorker>();


    private IConfiguration config;

    public TgMessageGrabbingWorker(ITdClientAuthorizer tdClientAuthorizer) {
        TdClientAuthorizer = tdClientAuthorizer;
        TdClient = TdClientAuthorizer.TdClient;
        tgRepository = new TelegramRepository(appSettings.SoanxConnectionString);
    }

    public async Task Run(CancellationToken cancellationToken) {
        log.Information("IN Run(). Grabbing settings: {@TgMessageGrabbingSettings}", appSettings.TgGrabbingSettings);
        await TdClientAuthorizer.Run();

        ConcurrentBag<TgMessageRaw> tgRawMessagesForStoring = new();
        List<Task> tasks = new List<Task>();
        var chatSettings = appSettings.TgGrabbingChats;
        foreach(var chatSetting in chatSettings) {
            tasks.Add(Task.Factory.StartNew(async () => 
                ReadTdMessagesIntoCollection(chatSetting, tgRawMessagesForStoring, cancellationToken)));
        }

        tasks.Add(SaveTgMessagesRaws(tgRawMessagesForStoring, cancellationToken));

        await Task.WhenAll(tasks);
        log.Information("OUT Run()");
    }

    public async Task ReadTdMessagesIntoCollection(TgGrabbingChat grabbingChat,
        ConcurrentBag<TgMessageRaw> collectionForAdding, CancellationToken cancellationToken) {

        var locLog = log.ForContext("method", "ReadTdMessagesIntoCollection()").ForContext("chatId", grabbingChat.ChatId);
        locLog.Information("IN");

        int minUnixDate = DateTimeHelper.ToUnixTime(DateTime.Now);
        var unixFromDate = DateTimeHelper.ToUnixTime(grabbingChat.ReadTillDate);
        long oldestMessageId = 0L;
        
        while (minUnixDate >= unixFromDate) {
            locLog.Information("TdClient.GetChatHistoryAsync() started... MinDate={@minDate}, fromMessageId={oldestMessageId}"
                , DateTimeHelper.FromUnixTime(minUnixDate), oldestMessageId);
            
            Messages msgBundle = await TdClient.GetChatHistoryAsync(
                chatId: grabbingChat.ChatId,
                fromMessageId: oldestMessageId,
                limit: appSettings.TgGrabbingSettings.ChatHistoryReadingCount,
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

                    if (!collectionForAdding.Any(adding =>
                        adding.TgChatId == tdMsg.ChatId && adding.TgChatMessageId == tdMsg.Id)) {
                        
                        var tgMessageRaw = MessageConverter.ConvertToTgMessageRaw(tdMsg);
                        collectionForAdding.Add(tgMessageRaw);
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
            Task.Delay(appSettings.TgGrabbingSettings.ReadingMessagesInterval).Wait();
        }
        locLog.Information("OUT");
    }

    private async Task SaveTgMessagesRaws(ConcurrentBag<TgMessageRaw> collectionForStoring, CancellationToken cancellationToken) {
        var locLog = log.ForContext("method", "SaveTgMessagesRaws()");
        locLog.Information("IN");

        while (!cancellationToken.IsCancellationRequested) {
            var messagesList = collectionForStoring.Take(appSettings.TgGrabbingSettings.SavingMessagesBatchSize).ToList();
            
            if (messagesList.Count > 0) {
                locLog.Information("msg in collectionForStoring = {allCollection}, taken to save = {takenCount}", collectionForStoring.Count, messagesList.Count);
                await tgRepository.SaveTgMessageRawList(messagesList);
            }
            Task.Delay(appSettings.TgGrabbingSettings.SavingMessagesRunsInterval).Wait();
        }
        locLog.Information("OUT. CancellationToken has been triggered");
    }
}