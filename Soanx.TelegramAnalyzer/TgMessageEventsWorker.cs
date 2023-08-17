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

    private ManualResetEvent resetEvent = new(false);
    private IConfiguration config;
    private TdLibParametersModel tdLibParameters;
    private AppSettingsHelper appSettings = new();
    private Serilog.ILogger log = Log.ForContext<TgMessageEventsWorker>();
    private HashSet<long> listeningChatIds = new();

    public TdClient TdClient { get; private set; }
    public ConcurrentBag<TgMessageRaw> CollectionForStoring { get; private set; }
    public List<TgListeningChat> TgListeningChats { get; private set; }

    public TgMessageEventsWorker(TdClient tdClient, ConcurrentBag<TgMessageRaw> collectionForStoring,
        List<TgListeningChat> tgListeningChats) {

        TdClient = tdClient;
        tgListeningChats.ForEach(c => listeningChatIds.Add(c.ChatId));
        CollectionForStoring = collectionForStoring;
        TgListeningChats = tgListeningChats;
    }

    public async Task Run(CancellationToken cancellationToken) {
        log.Information("IN Run(). Listening chats: {@TgListeningChats}", appSettings.TgListeningChats);
        cancellationToken.Register(() => resetEvent.Set());

        SubscribeToUpdateReceivedEvent();
        resetEvent.WaitOne();

        log.Information("OUT Run()");
    }

    public virtual void SubscribeToUpdateReceivedEvent() {
        TdClient.UpdateReceived += async (_, update) => { await UpdateReceived(update); };
    }
    
    protected async Task UpdateReceived(TdApi.Update update) {
        var locLog = log.ForContext("method", "UpdateReceived()").ForContext("update type", update);
        //locLog.Verbose("IN");

        switch (update) {
            case TdApi.Update.UpdateNewMessage:
                var newMsgUpdate = (UpdateNewMessage)update;
                
                if(!IsListeningChat(newMsgUpdate.Message.ChatId)) {
                    return;
                }
                await ProcessNewMessages((UpdateNewMessage)update);
                break;
            case TdApi.Update.UpdateMessageContent:
                var contentMsgUpdate = (UpdateMessageContent)update;

                if (!IsListeningChat(contentMsgUpdate.ChatId)) {
                    return;
                }
                await ProcessEditedMessage((UpdateMessageContent)update);
                break;
            case TdApi.Update.UpdateDeleteMessages:
                var deleteMsgUpdate = (UpdateDeleteMessages)update;

                if (!IsListeningChat(deleteMsgUpdate.ChatId)) {
                    return;
                }
                await ProcessDeletedMessages((UpdateDeleteMessages)update);
                break;
            default:
                break;
        }
    }

    private bool IsListeningChat(long chatId) {
        return listeningChatIds.Contains(chatId);
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