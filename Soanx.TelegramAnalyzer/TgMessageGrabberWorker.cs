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

namespace Soanx.TelegramAnalyzer;
public class TgMessageGrabbingWorker: ITelegramWorker {
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    //private readonly ConcurrentBag<TgMessageRaw> tgRawMessagesForStoring = new();

    private TdLibParametersModel tdLibParameters;
    private AppSettingsHelper appSettings = new();
    protected virtual TelegramRepository tgRepository { get; set; }

    public ITdClientAuthorizer TdClientAuthorizer { get; private set; }
    public TdClient TdClient { get; private set; }


    private IConfiguration config;

    public TgMessageGrabbingWorker(ITdClientAuthorizer tdClientAuthorizer) {
        TdClientAuthorizer = tdClientAuthorizer;
        TdClient = new TdClient();
        TdClient.Bindings.SetLogVerbosityLevel(TdLogLevel.Fatal);
        tgRepository = new TelegramRepository(appSettings.SoanxConnectionString);
    }

    public async Task Run(CancellationToken cancellationToken) {
        await TdClientAuthorizer.Run();

        ConcurrentBag<TgMessageRaw> tgRawMessagesForStoring = new();
        List<Task> tasks = new List<Task>();
        var chatSettings = appSettings.TgGrabbingChatsSettings;
        foreach(var chatSetting in chatSettings) {
            tasks.Add(Task.Factory.StartNew(async () => 
                ReadTdMessagesIntoCollection(chatSetting, tgRawMessagesForStoring, cancellationToken)));
        }

        tasks.Add(SaveTgMessagRaws(tgRawMessagesForStoring, cancellationToken));

        await Task.WhenAll(tasks);
    }

    public async Task ReadTdMessagesIntoCollection(TgGrabbingChatsSettings settings, 
        ConcurrentBag<TgMessageRaw> collectionForAdding, CancellationToken cancellationToken) {

        //var chats = await TdClient.ExecuteAsync(new TdApi.GetChats {
        //    Limit = 100
        //});
        int minUnixDate = DateTimeHelper.ToUnixTime(DateTime.Now);
        var unixFromDate = DateTimeHelper.ToUnixTime(settings.DateFrom);

        while (minUnixDate >= unixFromDate) {
            Messages msgBundle = await TdClient.GetChatHistoryAsync(settings.ChatId, limit: 100, onlyLocal: false);
            minUnixDate = msgBundle.Messages_.Min(m => m.Date);
            
            for (int i = 0; i < msgBundle.Messages_.Count(); i++) {
                collectionForAdding.Add(MessageConverter.ConvertToTgMessageRaw(msgBundle.Messages_[i]));
            }

            if(cancellationToken.IsCancellationRequested) {
                return;
            }
            Task.Delay(500).Wait();
        }
    }

    private async Task SaveTgMessagRaws(ConcurrentBag<TgMessageRaw> collectionForStoring, CancellationToken cancellationToken) {

        while (!cancellationToken.IsCancellationRequested) {
            var messagesList = collectionForStoring.Take(300).ToList();
            if (messagesList.Count > 0) {
                await tgRepository.SaveTgMessageRawListAsync(messagesList);
            }
        }
    }
}