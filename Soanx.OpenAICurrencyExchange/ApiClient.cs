using OpenAI.GPT3.Managers;
using OpenAI.GPT3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels;

namespace Soanx.OpenAICurrencyExchange;

public class OpenAiParameters {
    public required string OpenAiApiKey { get; set; }
}

public class ApiClient {
    private OpenAiParameters OpenAiParameters;
    private OpenAIService openAiService;

    public ApiClient(OpenAiParameters openAiParameters) {
        this.OpenAiParameters = openAiParameters;

        openAiService = new OpenAIService(new OpenAiOptions() {
            ApiKey = OpenAiParameters.OpenAiApiKey
        });
    }

    public async Task FormalizeExchangeMessages() {
        try {
            var completionRequest = new CompletionCreateRequest() {
                Prompt = "Once upon a time",
                Model = OpenAI.GPT3.ObjectModels.Models.TextDavinciV3
            };
            var completionResult = await openAiService.Completions.CreateCompletion(completionRequest);
            int t = 1;
        } catch (Exception ex) {
            int r = 5;
        }
    }
}



