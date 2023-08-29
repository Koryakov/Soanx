using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels.SharedModels;
using Serilog;
using Soanx.CurrencyExchange;
using Soanx.CurrencyExchange.OpenAiDtoModels;
using System.Reflection.Metadata.Ecma335;

namespace Soanx.TelegramAnalyzer;
public class OpenAiApiClient {

    private OpenAIService openAiService;
    private string openAiApiKey;
    private string openAiModelName;
    private Serilog.ILogger log = Log.ForContext<OpenAiApiClient>();

    public OpenAiApiClient(string openAiApiKey, string openAiModelName) {
        var locLog = log.ForContext("method", "constructor");
        locLog.Information("IN. openAiModelName: {@openAiModelName}", openAiModelName);

        this.openAiApiKey = openAiApiKey;
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

    public async Task<bool> SendPromptRequestAndValidateResult() {
        var locLog = log.ForContext("method", "SendPromptRequestAndValidateResult");
        locLog.Information("IN");

        var mneExchangePromptHelper = await ChatPromptHelper.CreateNew("MontenegroExchange");

        var openAiRequest = new ChatCompletionCreateRequest();
        openAiRequest.Messages = new List<ChatMessage>
            {
                ChatMessage.FromSystem(mneExchangePromptHelper.Instruction),
                ChatMessage.FromSystem(mneExchangePromptHelper.ResultSchemaJson),
                ChatMessage.FromUser(mneExchangePromptHelper.MessagesJson),
                ChatMessage.FromAssistant(mneExchangePromptHelper.FormalizedMessagesJson),
            };
        openAiRequest.Model = openAiModelName;

        ChatChoiceResult result = await SendOpenAiRequest(openAiRequest);

        locLog.Information("OUT. SendOpenAiRequest result.IsSuccess = {@IsSuccess}, result.Choices: {@choices}", 
            result.IsSuccess, result.Choices);

        return result.IsSuccess;
    }

    public async Task<ChatChoiceResult> SendOpenAiRequest(List<MessageForAnalyzing> messages) {
        var locLog = log.ForContext("method", "SendOpenAiRequest(List<MessageForAnalyzing> messages)");
        locLog.Information("IN. MessageForAnalyzing count = {@msgListCount}", messages.Count);

        var openAiRequest = new ChatCompletionCreateRequest() { Model = openAiModelName };
        openAiRequest.Messages = new List<ChatMessage>();
        var jsonMessages = SerializationHelper.Serialize<List<MessageForAnalyzing>>(messages);
        openAiRequest.Messages.Add(ChatMessage.FromUser(jsonMessages));

        var result = await SendOpenAiRequest(openAiRequest);
        locLog.Verbose<ChatChoiceResult>("{@ChatChoiceResult}", result);
        return result;
    }

    public async Task<ChatChoiceResult> SendOpenAiRequest(ChatCompletionCreateRequest request) {
        var locLog = log.ForContext("method", "SendOpenAiRequest(ChatCompletionCreateRequest request)");
        locLog.Information("IN. request.Messages.Count = {@msgListCount}", request.Messages.Count);
        
        bool isSuccess = false;
        List<ChatChoiceResponse>? answers = new ();

        try {
            var completionResult = await openAiService.ChatCompletion.CreateCompletion(request);
            if (completionResult.Successful) {
                foreach (var choice in completionResult.Choices) {
                    answers.Add(choice);
                }
                isSuccess = request.Messages.Count > 0 && completionResult.Choices.Count > 0;
            }
            else {
                if (completionResult.Error == null) {
                    throw new Exception("Unknown Error");
                }
            }
        }
        catch (Exception ex) {
            locLog.Error(ex, "messages have been returned to the collectionForAnalyzing.");
        }
        locLog.Information("OUT. Responded choices count = {@chCount}", answers.Count);
        return new ChatChoiceResult() { IsSuccess = isSuccess, Choices = answers };
    }
}