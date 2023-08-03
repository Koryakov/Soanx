using static TdLib.TdApi.Update;
using static TdLib.TdApi;
using TdLib.Bindings;
using TdLib;
using Soanx.TelegramModels;
using Soanx.Repositories.Models;
using System.Text.Json;
using Soanx.Repositories;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Soanx.TgWorker;

namespace Soanx.TelegramAnalyzer;
public class TgMessageEventsWorker: ITelegramWorker {
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    private TdLibParametersModel tdLibParameters;
    private AppSettingsHelper appSettings = new();
    public ITdClientAuthorizer TdClientAuthorizer { get; private set; }
    public TdClient TdClient { get; private set; }

    private IConfiguration config;

    public TgMessageEventsWorker(ITdClientAuthorizer tdClientAuthorizer) {
        TdClientAuthorizer = tdClientAuthorizer;
        TdClient = new TdClient();
        TdClient.Bindings.SetLogVerbosityLevel(TdLogLevel.Fatal);
    }

    public async Task Run(CancellationToken cancellationToken) {
        SubscribeToUpdateReceivedEvent();
        await TdClientAuthorizer.Run();
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
    }
    private async Task ProcessEditedMessage(UpdateMessageContent update) {
    }
    private async Task ProcessDeletedMessages(UpdateDeleteMessages update) {
     
    }
    private async Task ProcessMessagesUpdates(TdApi.Update update) {
    }

}