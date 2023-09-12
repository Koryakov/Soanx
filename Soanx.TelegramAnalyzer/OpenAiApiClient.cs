using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels.ResponseModels;
using OpenAI.ObjectModels.SharedModels;
using Serilog;
using Soanx.CurrencyExchange;
using Soanx.CurrencyExchange.Models;
using System.Reflection.Metadata.Ecma335;

namespace Soanx.TelegramAnalyzer;
public class OpenAiApiClient {

    private OpenAIService openAiService;
    private string openAiApiKey;
    private string openAiModelName;
    private Serilog.ILogger log = Log.ForContext<OpenAiApiClient>();
    private ChatPromptHelper chatPromptHelper;


    public OpenAiApiClient(string openAiApiKey, ChatPromptHelper chatPromptHelper, string openAiModelName) {
        var locLog = log.ForContext("method", "constructor");
        locLog.Information("IN. openAiModelName: {@openAiModelName}", openAiModelName);

        this.openAiApiKey = openAiApiKey;
        this.chatPromptHelper = chatPromptHelper;
        this.openAiModelName = openAiModelName;
        InitializeOpenService(openAiApiKey);
    }

    private OpenAIService InitializeOpenService(string openAiApiKey) {
        var locLog = log.ForContext("method", "InitializeOpenService");

        HttpClient httpClient = new HttpClient() { Timeout = new TimeSpan(0, 5, 0) };
        openAiService = new OpenAIService(
            new OpenAiOptions() { ApiKey = openAiApiKey },
            httpClient
        );
        locLog.Information("HttpClient created. Timeout = {@timeout}. OpenAIService initialized.", httpClient.Timeout);
        return openAiService;
    }

    //public async Task<bool> SendPromptRequestAndValidateResult() {
    //    var locLog = log.ForContext("method", "SendPromptRequestAndValidateResult");
    //    locLog.Information("IN");

    //    var mneExchangePromptHelper = await ChatPromptHelper.CreateNew("MontenegroExchange");

    //    var openAiRequest = new ChatCompletionCreateRequest();
    //    openAiRequest.Messages = new List<ChatMessage>
    //        {
    //            ChatMessage.FromSystem(mneExchangePromptHelper.Instruction),
    //            ChatMessage.FromSystem(mneExchangePromptHelper.ResultSchemaJson),
    //            ChatMessage.FromUser(mneExchangePromptHelper.MessagesJson),
    //            ChatMessage.FromAssistant(mneExchangePromptHelper.FormalizedMessagesJson),
    //            ChatMessage.FromUser(),
    //        };
    //    openAiRequest.Model = openAiModelName;

    //    ChatChoiceResult result = await SendOpenAiRequest(openAiRequest);

    //    locLog.Information("OUT. SendOpenAiRequest result.IsSuccess = {@IsSuccess}, result.Choices: {@choices}", 
    //        result.IsSuccess, result.Choices);

    //    return result.IsSuccess;
    //}

    public async Task<ChatChoiceResult> SendOpenAiRequest(List<DtoModels.MessageForAnalyzing> messages) {
        var locLog = log.ForContext("method", "SendOpenAiRequest(List<MessageForAnalyzing> messages)");
        locLog.Debug("IN. MessageForAnalyzing count = {@msgListCount}", messages.Count);

        var openAiRequest = new ChatCompletionCreateRequest() { Model = openAiModelName };
        openAiRequest.Messages = chatPromptHelper.PromptingSetList;
        var jsonMessages = SerializationHelper.Serialize<List<DtoModels.MessageForAnalyzing>>(messages);
        openAiRequest.Messages.Add(ChatMessage.FromUser(jsonMessages));

        var result = await SendOpenAiRequest(openAiRequest);
        locLog.Debug("OUT.");
        return result;
    }

    private async Task<ChatChoiceResult> SendOpenAiRequest(ChatCompletionCreateRequest completionRequest) {
        var locLog = log.ForContext("method", "SendOpenAiRequest(ChatCompletionCreateRequest request)");
        locLog.Debug("IN. request.Messages.Count = {@msgListCount}", completionRequest.Messages.Count);
        locLog.Verbose<ChatCompletionCreateRequest>("completionRequest: {@completionRequest}", completionRequest);
        
        bool isSuccess = false;
        List<ChatChoiceResponse>? answers = new();

        var completionResult = await openAiService.ChatCompletion.CreateCompletion(completionRequest);
        if (completionResult.Successful) {
            foreach (var choice in completionResult.Choices) {
                answers.Add(choice);
            }
            isSuccess = completionRequest.Messages.Count > 0 && completionResult.Choices.Count > 0;
        }
        else {
            locLog.Error<ChatCompletionCreateResponse>("Error. completionResult: {@completionResult}", completionResult);
        }
        locLog.Verbose<ChatCompletionCreateResponse>("completionResult: {@completionResult}", completionResult);
        locLog.Debug("OUT. Responded choices count = {@chCount}", answers.Count);
        return new ChatChoiceResult() { IsSuccess = isSuccess, Choices = answers };
    }
}