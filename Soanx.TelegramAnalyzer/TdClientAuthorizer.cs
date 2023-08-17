using Microsoft.Extensions.Logging;
using Serilog;
using Soanx.TelegramAnalyzer.Models;
using Soanx.TelegramModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TdLib;
using Telegram.Bot;

namespace Soanx.TelegramAnalyzer;

public class TdClientAuthorizer : ITdClientAuthorizer {
    
    private bool authNeeded;
    private bool passwordNeeded;
    private EventHandler<TdApi.Update> updateReceivedHandler;
    private readonly ManualResetEventSlim ReadyToAuthenticate = new();
    //private readonly ManualResetEventSlim ReadyToFinish = new();
    public TdClient TdClient { get; private set; }
    public TdLibParametersModel TdLibParameters { get; private set; }
    public TelegramBotSettings BotSettings { get; private set; }
    private Serilog.ILogger log;


    public TdClientAuthorizer(TdClient tdClient, TdLibParametersModel tdLibParameters, TelegramBotSettings botSettings) {
        log = Log.ForContext<TdClientAuthorizer>();
        TdClient = tdClient;
        TdLibParameters = tdLibParameters;
        BotSettings = botSettings;
    }
    public async Task Run() {
        log.Information("Run() started...");
        SubscribeToUpdateReceivedEvent();
        ReadyToAuthenticate.Wait();

        if (authNeeded) {
            await HandleAuthentication();
        }
        UnsubscribeToUpdateReceivedEvent();
        log.Information("Run() ended");
    }

    public virtual void SubscribeToUpdateReceivedEvent() {
        log.Information("SubscribeToUpdateReceivedEvent()...");
        updateReceivedHandler = async (_, update) => { await UpdateReceived(update); };
        TdClient.UpdateReceived += updateReceivedHandler;
    }

    public virtual void UnsubscribeToUpdateReceivedEvent() {
        if (updateReceivedHandler != null) {
            log.Information("UnsubscribeToUpdateReceivedEvent()...");
            TdClient.UpdateReceived -= updateReceivedHandler;
            updateReceivedHandler = null;
        }
    }

    public virtual async Task HandleAuthentication() {
        log.Information("HandleAuthentication() started...");
        
        await TdClient.ExecuteAsync(new TdApi.SetAuthenticationPhoneNumber {
            PhoneNumber = TdLibParameters.PhoneNumber
        });
        log.Information("TdApi.SetAuthenticationPhoneNumber called, PhoneNumber={PhoneNumber}", TdLibParameters.PhoneNumber);


        TelegramBotHelper botHelper = new(BotSettings);
        string smsCode = await botHelper.SendSmsCodeRequest("Send SMS code from phone");
        
        await TdClient.ExecuteAsync(new TdApi.CheckAuthenticationCode {
            Code = smsCode
        });
        log.Information("TdApi.CheckAuthenticationCode called, Code={@smsCode}", smsCode);

        if (!passwordNeeded) { 
            log.Information($"passwordNeeded={@passwordNeeded}, return");
            return;
        }

        string password = await botHelper.SendSmsCodeRequest("Send password");

        await TdClient.ExecuteAsync(new TdApi.CheckAuthenticationPassword {
            Password = password
        });
        log.Information("TdApi.CheckAuthenticationPassword called, Password={@password}", password);

        log.Information("HandleAuthentication() ended");
    }

    public async Task UpdateReceived(TdApi.Update update) {
        if(!authNeeded || !passwordNeeded) {
            log.Information("UpdateReceived started, authNeeded={0}, passwordNeeded={1}, update={update}", authNeeded, passwordNeeded, update);
        }
        switch (update) {
            case TdApi.Update.UpdateAuthorizationState { AuthorizationState: TdApi.AuthorizationState.AuthorizationStateWaitTdlibParameters }:
                await Authorize();
                break;

            case TdApi.Update.UpdateAuthorizationState { AuthorizationState: TdApi.AuthorizationState.AuthorizationStateWaitPhoneNumber }:
            case TdApi.Update.UpdateAuthorizationState { AuthorizationState: TdApi.AuthorizationState.AuthorizationStateWaitCode }:
                authNeeded = true;
                SetReadyToAuthenticate();
                break;

            case TdApi.Update.UpdateAuthorizationState { AuthorizationState: TdApi.AuthorizationState.AuthorizationStateWaitPassword }:
                authNeeded = true;
                passwordNeeded = true;
                SetReadyToAuthenticate();
                break;

            case TdApi.Update.UpdateUser:
                SetReadyToAuthenticate();
                break;
         
            default:
                break;
        }
    }

    private void SetReadyToAuthenticate() {
        ReadyToAuthenticate.Set();
        log.Verbose("ReadyToAuthenticate has been set.");
    }

    public async Task Authorize() {
        log.Information("Authorize() started");
        await TdClient.ExecuteAsync(new TdApi.SetTdlibParameters {
            ApiId = TdLibParameters.ApiId,
            ApiHash = TdLibParameters.ApiHash,
            DeviceModel = TdLibParameters.DeviceModel,
            SystemLanguageCode = TdLibParameters.SystemLanguageCode,
            ApplicationVersion = TdLibParameters.ApplicationVersion,
            DatabaseDirectory = TdLibParameters.DatabaseDirectory,
            FilesDirectory = TdLibParameters.FilesDirectory,
            // More parameters available!
        });
        log.Information("Authorize() ended");
    }

}
