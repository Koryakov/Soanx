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
    private readonly ManualResetEventSlim ReadyToAuthenticate = new();
    private readonly ManualResetEventSlim ReadyToFinish = new();
    public TdClient TdClient { get; private set; }
    public TdLibParametersModel TdLibParameters { get; private set; }
    public TelegramBotSettings BotSettings { get; private set; }


    public TdClientAuthorizer(TdClient tdClient, TdLibParametersModel tdLibParameters, TelegramBotSettings botSettings) {
        TdClient = tdClient;
        TdLibParameters = tdLibParameters;
        BotSettings = botSettings;
    }
    public async Task Run() {
        SubscribeToUpdateReceivedEvent();
        ReadyToAuthenticate.Wait();

        if (authNeeded) {
            await HandleAuthentication();
        }
    }

    public virtual void SubscribeToUpdateReceivedEvent() {
        TdClient.UpdateReceived += async (_, update) => { await UpdateReceived(update); };
    }

    public virtual async Task HandleAuthentication() {
        await TdClient.ExecuteAsync(new TdApi.SetAuthenticationPhoneNumber {
            PhoneNumber = TdLibParameters.PhoneNumber
        });

        TelegramBotHelper botHelper = new(BotSettings);
        string smsCode = await botHelper.SendSmsCodeRequest("Send SMS code from phone");
        
        await TdClient.ExecuteAsync(new TdApi.CheckAuthenticationCode {
            Code = smsCode
        });

        if (!passwordNeeded) { return; }

        string password = await botHelper.SendSmsCodeRequest("Send password");

        await TdClient.ExecuteAsync(new TdApi.CheckAuthenticationPassword {
            Password = password
        });
    }

    public async Task UpdateReceived(TdApi.Update update) {
        //Console.WriteLine($"{update.GetType()}");

        switch (update) {
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
         
            default:
                break;
        }
    }

    public async Task Authorize() {
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
    }

}
