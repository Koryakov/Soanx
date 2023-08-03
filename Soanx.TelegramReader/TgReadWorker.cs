using static TdLib.TdApi.Update;
using static TdLib.TdApi;
using TdLib.Bindings;
using TdLib;
using Serilog;
using Soanx.TelegramModels;

namespace Soanx.TelegramReader;
public class TgReadWorker {
    private readonly ILogger log = Log.ForContext<TgReadWorker>();

    private TdLibParametersModel tdLibParameters;
    private TgWorkerManager workerManager;

    private TdClient tdClient;
    private readonly ManualResetEventSlim ReadyToAuthenticate = new();

    private bool authNeeded;
    private bool passwordNeeded;

    private IConfiguration config;
    private string soanxConnectionString;
    private ApiClient openAiApiClient;

    private bool ignoreMessagesEvents;
    protected virtual TelegramRepository tgRepository { get; set; }

    public TgEngine() {
        config = new ConfigurationBuilder().AddJsonFile("appsettings.json").AddEnvironmentVariables().Build();
        tdLibParameters = config.GetRequiredSection("TdLibParameters").Get<TdLibParametersModel>();
        soanxConnectionString = config.GetConnectionString("SoanxDbConnection");
        tgRepository = new TelegramRepository(soanxConnectionString);

        tdClient = new TdClient();
    }
    public virtual void SubscribeToUpdateReceivedEvent() {
        tdClient.UpdateReceived += async (_, update) => { await UpdateReceived(update); };
    }

    public async Task RunAsync(bool ignoreMessagesEvents = false) {
        this.ignoreMessagesEvents = ignoreMessagesEvents;

        this.tdClient = new TdClient();
        tdClient.Bindings.SetLogVerbosityLevel(TdLogLevel.Fatal);

        workerManager = new TgWorkerManager();
        await workerManager.LoadWorkersAssemblies();

        SubscribeToUpdateReceivedEvent();
        ReadyToAuthenticate.Wait();
        log.Info("TdLib authentication is done");

        if (authNeeded) {
            log.Info($"TdLib authNeeded = {@authNeeded}");
            await HandleAuthentication();
        }
    }


    public async Task<List<Message>> GetTdMessages(long chatId, DateTime dtFrom) {

        var chats = await tdClient.ExecuteAsync(new TdApi.GetChats {
            Limit = 100
        });

        int minUnixDate = DateTimeHelper.ToUnixTime(DateTime.Now);
        var unixFromDate = DateTimeHelper.ToUnixTime(dtFrom);
        List<Message> tdMessages = new();
        do {
            Messages msgBundle = await tdClient.GetChatHistoryAsync(chatId, limit: 100, onlyLocal: false);
            minUnixDate = msgBundle.Messages_.Min(m => m.Date);
            tdMessages.AddRange(msgBundle.Messages_);
            Task.Delay(500).Wait();
        } while (minUnixDate >= unixFromDate || tdMessages.Count <= 100);

        return tdMessages;
    }

    public List<TgMessage> ConvertToSoanxMessages(List<Message> rawMessages) {
        List<TgMessage> convertedMessages = new();
        var textMessages = rawMessages.Where(r => r.Content.DataType == "messageText").ToList();
        foreach (var txtMessage in textMessages) {
            convertedMessages.Add(MessageConverter
                .ConvertTgMessage(txtMessage, UpdateType.None, JsonSerializer.Serialize(txtMessage)));
        }
        return convertedMessages;
    }

    public async Task<int> SaveTgMessages(List<TgMessage> tgMessages) {
        return await tgRepository.AddTgMessageAsync(tgMessages);
    }

    public async Task<List<TgMessage>> LoadTgMessagesFromDbAsync(long chatId, DateTime sinceDate) {
        //Temporary solution. Specific worker plugin chats must be specified.
        List<TgMessage> tgMessages = await tgRepository.GetLastNotExtractedTgMessages(chatId, sinceDate, 20);

        return tgMessages;
    }

    protected async Task UpdateReceived(TdApi.Update update) {
        log.Info($"IN UpdateReceived({update.GetType()})");

        switch (update) {
            case TdApi.Update.UpdateNewMessage:
                if (!ignoreMessagesEvents) {
                    await ProcessNewMessages((UpdateNewMessage)update);
                }
                break;
            case TdApi.Update.UpdateMessageContent:
                if (!ignoreMessagesEvents) {
                    await ProcessEditedMessage((UpdateMessageContent)update);
                }
                break;
            case TdApi.Update.UpdateDeleteMessages:
                if (!ignoreMessagesEvents) {
                    await ProcessDeletedMessages((UpdateDeleteMessages)update);
                }
                break;
            case TdApi.Update.UpdateAuthorizationState { AuthorizationState: TdApi.AuthorizationState.AuthorizationStateWaitTdlibParameters }:
                await Authorize();
                break;

            case TdApi.Update.UpdateAuthorizationState { AuthorizationState: TdApi.AuthorizationState.AuthorizationStateWaitPhoneNumber }:
            case TdApi.Update.UpdateAuthorizationState { AuthorizationState: TdApi.AuthorizationState.AuthorizationStateWaitCode }:
                authNeeded = true;
                ReadyToAuthenticate.Set();
                break;

            case TdApi.Update.UpdateAuthorizationState { AuthorizationState: TdApi.AuthorizationState.AuthorizationStateWaitPassword }:
                authNeeded = true;
                passwordNeeded = true;
                ReadyToAuthenticate.Set();
                break;

            case TdApi.Update.UpdateUser:
                ReadyToAuthenticate.Set();
                break;
            case TdApi.Update.UpdateConnectionState { State: TdApi.ConnectionState.ConnectionStateReady }:
                break;

            default:
                break;
        }
    }
    private async Task ProcessNewMessages(UpdateNewMessage update) {
        log.Trace($"IN ProcessNewMessages");
        try {
            //TODO: store messages to Queue
            TgMessage tgMessage = MessageConverter.ConvertTgMessage(update.Message, UpdateType.UpdateNewMessage, JsonSerializer.Serialize(update));
            await tgRepository.AddTgMessageAsync(tgMessage);

            IEnumerable<ITgWorker> workerInstances = workerManager.CreateWorkersForEvent(update.Message.ChatId, UpdateType.UpdateNewMessage);
            foreach (var instance in workerInstances) {
                await instance.Run();
            }
            log.Trace($"OUT ProcessNewMessages");
        }
        catch (Exception ex) {
            log.Error(ex, $"ProcessNewMessages Error");
        }
    }


    private async Task ProcessEditedMessage(UpdateMessageContent update) {
        IEnumerable<ITgWorker> workerInstances = workerManager.CreateWorkersForEvent(update.ChatId, UpdateType.UpdateMessageContent);
        foreach (var instance in workerInstances) {
            await instance.Run();
        }
    }
    private async Task ProcessDeletedMessages(UpdateDeleteMessages update) {
        IEnumerable<ITgWorker> workerInstances = workerManager.CreateWorkersForEvent(update.ChatId, UpdateType.UpdateDeleteMessages);
        foreach (var instance in workerInstances) {
            //TODO: workers shoud work parallels
            /*await*/
            instance.Run();
            //await Task.Run( () => instance.Run());
        }
    }

    private async Task ProcessMessagesUpdates(TdApi.Update update) {
        IEnumerable<ITgWorker> workerInstances = workerManager.CreateWorkersForEvent(1, UpdateType.UpdateNewMessage);
    }

    public virtual async Task HandleAuthentication() {
        // Setting phone number
        await tdClient.ExecuteAsync(new TdApi.SetAuthenticationPhoneNumber {
            PhoneNumber = tdLibParameters.PhoneNumber
        });

        // Telegram servers will send code to us
        Console.Write("Insert the login code: ");
        var code = Console.ReadLine();

        await tdClient.ExecuteAsync(new TdApi.CheckAuthenticationCode {
            Code = code
        });

        if (!passwordNeeded) { return; }

        // 2FA may be enabled. Cloud password is required in that case.
        Console.Write("Insert the password: ");
        var password = Console.ReadLine();

        await tdClient.ExecuteAsync(new TdApi.CheckAuthenticationPassword {
            Password = password
        });
    }

    private async Task Authorize() {
        await tdClient.ExecuteAsync(new TdApi.SetTdlibParameters {
            ApiId = tdLibParameters.ApiId,
            ApiHash = tdLibParameters.ApiHash,
            DeviceModel = tdLibParameters.DeviceModel,
            SystemLanguageCode = tdLibParameters.SystemLanguageCode,
            ApplicationVersion = tdLibParameters.ApplicationVersion,
            DatabaseDirectory = tdLibParameters.DatabaseDirectory,
            FilesDirectory = tdLibParameters.FilesDirectory,
            // More parameters available!
        });
    }

    private async Task<TdApi.User> GetCurrentUser() {
        return await tdClient.ExecuteAsync(new TdApi.GetMe());
    }

    public async Task TypeChatMessages() {
        var channels = GetChannels(40);

        await foreach (var channel in channels) {
            Console.WriteLine($"[{channel.Id}] -> [{channel.Title}] ({channel.UnreadCount} messages unread)");

            Messages messages = await tdClient.GetChatHistoryAsync(chatId: channel.Id, limit: 10);
            foreach (Message msg in messages.Messages_) {
                if (msg.Content.DataType == "messageText") {
                    //var user = await tdClient.GetUserAsync(((MessageSenderUser)msg.SenderId).UserId);
                    //Console.WriteLine(((MessageText)msg.Content).Text.Text);
                }
            }
        }
    }

    private async IAsyncEnumerable<TdApi.Chat> GetChannels(int limit) {
        var chats = await tdClient.ExecuteAsync(new TdApi.GetChats {
            Limit = limit
        });

        foreach (var chatId in chats.ChatIds) {
            var chat = await tdClient.ExecuteAsync(new TdApi.GetChat {
                ChatId = chatId
            });

            if ((chat.Type is TdApi.ChatType.ChatTypeSupergroup or TdApi.ChatType.ChatTypeBasicGroup or TdApi.ChatType.ChatTypePrivate)
                ) {
                yield return chat;
            }
        }
    }
}