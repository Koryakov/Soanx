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
public class TgMessageEventsWorker: ITelegramWorker {
    private IConfiguration config;
    private TdLibParametersModel tdLibParameters;
    private AppSettingsHelper appSettings = new();
    private Serilog.ILogger log = Log.ForContext<TgMessageEventsWorker>();
    private ConcurrentBag<TgMessageRaw> tgRawMessagesForStoring = new();
    protected virtual TelegramRepository tgRepository { get; set; }
    public ITdClientAuthorizer TdClientAuthorizer { get; private set; }
    public TdClient TdClient { get; private set; }
    public ConcurrentBag<TgMessageRaw> CollectionForStoring { get; private set; }

    public TgMessageEventsWorker(ITdClientAuthorizer tdClientAuthorizer, ConcurrentBag<TgMessageRaw> collectionForStoring) {
        TdClientAuthorizer = tdClientAuthorizer;
        TdClient = TdClientAuthorizer.TdClient;
        CollectionForStoring = collectionForStoring;
        tgRepository = new TelegramRepository(appSettings.SoanxConnectionString);
    }

    public async Task Run(CancellationToken cancellationToken) {
        log.Information("IN Run(). Listening chats: {@TgListeningChats}", appSettings.TgListeningChats);

        SubscribeToUpdateReceivedEvent();
        await TdClientAuthorizer.Run();
        
        log.Information("OUT Run()");
    }

    public virtual void SubscribeToUpdateReceivedEvent() {
        TdClient.UpdateReceived += async (_, update) => { await UpdateReceived(update); };
    }

    protected async Task UpdateReceived(TdApi.Update update) {
        Console.WriteLine($"{update.GetType()}");

        switch (update) {
            case TdApi.Update.UpdateNewMessage:
                await ProcessNewMessages((UpdateNewMessage)update);
                break;
            case TdApi.Update.UpdateMessageContent:
                await ProcessEditedMessage((UpdateMessageContent)update);
                break;
            case TdApi.Update.UpdateDeleteMessages:
                await ProcessDeletedMessages((UpdateDeleteMessages)update);
                break;
            default:
                break;
        }
    }

    private async Task ProcessNewMessages(UpdateNewMessage update) {
        var locLog = log.ForContext("method", "ProcessNewMessages()");
        try {
            var tgMessageRaw = MessageConverter.ConvertToTgMessageRaw(update);
            CollectionForStoring.Add(tgMessageRaw);
        }
        catch (Exception ex) {
            locLog.Error(ex, $"Error");
        }
    }

    private async Task ProcessEditedMessage(UpdateMessageContent update) {
        var locLog = log.ForContext("method", "UpdateMessageContent()");
        try {
        }
        catch (Exception ex) {
            locLog.Error(ex, $"Error");
        }
    }
    
    private async Task ProcessDeletedMessages(UpdateDeleteMessages update) {
     
    }

    private async Task ProcessMessagesUpdates(TdApi.Update update) {
    }

}