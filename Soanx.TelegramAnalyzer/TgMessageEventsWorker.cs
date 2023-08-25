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
    private Serilog.ILogger log = Log.ForContext<TgMessageEventsWorker>();
    private HashSet<long> tgListeningChats = new();
    private TgRepository tgRepository;

    public TdClient TdClient { get; private set; }
    public List<TgListeningChat> TgListeningChats { get; private set; }

    public TgMessageEventsWorker(TdClient tdClient, List<TgListeningChat> tgListeningChats,
        string soanxConnectionString) {

        TdClient = tdClient;
        tgListeningChats.ForEach(c => this.tgListeningChats.Add(c.ChatId));
        TgListeningChats = tgListeningChats;
        tgRepository = new TgRepository(soanxConnectionString);
    }

    public async Task Run(CancellationToken cancellationToken) {
        log.Information("IN Run(). Listening chats: {@TgListeningChats}", tgListeningChats);
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

        switch (update) {
            case TdApi.Update.UpdateNewMessage:
                var newMsgUpdate = (UpdateNewMessage)update;
                
                if(!IsListeningChat(newMsgUpdate.Message.ChatId)) {
                    return;
                }
                await ProcessNewMessage((UpdateNewMessage)update);
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
        return tgListeningChats.Contains(chatId);
    }


    private async Task ProcessNewMessage(UpdateNewMessage update) {
        var locLog = log.ForContext("method", "ProcessNewMessage()");
        locLog.Information("IN");
        try {
            var tgMessage = MessageConverter.ConvertToTgMessage(update);
            await tgRepository.AddTgMessage(tgMessage);
            
        }
        catch (Exception ex) {
            locLog.Error(ex, $"Error");
        }
    }

    private async Task ProcessEditedMessage(UpdateMessageContent updateContent) {
        var locLog = log.ForContext("method", "ProcessEditedMessage()");
        locLog.Information("IN");

        try {
            var tgMessage = MessageConverter.ConvertToTgMessage(updateContent);
            await tgRepository.AddTgMessage(tgMessage);
        }
        catch (Exception ex) {
            locLog.Error(ex, $"Error");
        }
    }
    
    private async Task ProcessDeletedMessages(UpdateDeleteMessages delete) {
        var locLog = log.ForContext("method", "ProcessDeletedMessages()");
        locLog.Information("IN");
        try {
            var tgMessageList = MessageConverter.ConvertToTgMessageCollection(delete);
            await tgRepository.AddTgMessage(tgMessageList);
        }
        catch (Exception ex) {
            locLog.Error(ex, $"Error");
        }
    }

}