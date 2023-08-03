using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OpenAI.ObjectModels.RequestModels;
using Soanx.OpenAICurrencyExchange.Models;

namespace Soanx.OpenAICurrencyExchange;

public class OpenAiParameters {
    public required string OpenAiApiKey { get; set; }
}

public class ApiClient {
    private OpenAiParameters OpenAiParameters;
    private OpenAIService openAiService;
    public string PromptCollectionName { get; private set; }

    public ApiClient(OpenAiParameters openAiParameters, string promptCollectionName) {
        this.OpenAiParameters = openAiParameters;
        PromptCollectionName = promptCollectionName;

        openAiService = new OpenAIService(new OpenAiOptions() {
            ApiKey = OpenAiParameters.OpenAiApiKey
        });
    }

    public async Task<(bool Success, string? FormalizedMessages)> FormalizeExchangeMessages(List<string> rawMessages) {
        var promptHelper = new ChatPromptHelper();
        await promptHelper.InitializePromptCollections(PromptCollectionName);//"MontenegroExchange"

        var request = new ChatCompletionCreateRequest {
            Messages = new List<ChatMessage>
            {
                ChatMessage.FromUser(promptHelper.Instruction),
                ChatMessage.FromUser(promptHelper.ResultSchemaJson),
                ChatMessage.FromUser(promptHelper.MessagesJson),
                ChatMessage.FromAssistant(promptHelper.FormalizedMessagesJson),

                ChatMessage.FromUser($"[{{\"Id\": 123,\"Message\": \"📍Бар.\\r\\n-Выдадим наличные EUR за Ваши:\\r\\nБезналичные рубли 92 - 92.7\\r\\nБезналичные гривны 43 - 44\\r\\nНаличные доллары 0,89 - 0.9\\r\\n-Купим Вашу криптовалюту:\\r\\nUSDT/USDC/BUSD 0,895 - 0,905\"}}]"),
            },
            Model = OpenAI.ObjectModels.Models.ChatGpt3_5Turbo, 
        };

        try {
            var result = await openAiService.ChatCompletion.CreateCompletion(request);
            if (result.Successful) {
                return new (true, result.Choices.First().Message.Content);
            }
            else {
                //TODO: add log
                return new (false, null);
            }
        }
        catch (Exception ex) {
            //TODO: add log
            return new (false, null);
        }
    }
}



