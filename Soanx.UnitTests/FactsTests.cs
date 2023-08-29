using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Moq;
using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;
using Soanx.CurrencyExchange;
using Soanx.TelegramAnalyzer;
using Soanx.TelegramAnalyzer.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TdLib;
using static System.Net.Mime.MediaTypeNames;
using static TdLib.TdApi;
using static TdLib.TdApi.MessageContent;
using static TdLib.TdApi.Update;

namespace Soanx.UnitTests;
public class FactsTests {

    private TgEngine tgEngine;
    private IConfiguration config;

    public FactsTests() {
        config = new ConfigurationBuilder().AddJsonFile("appsettings.json").AddEnvironmentVariables().Build();
        tgEngine = new TgEngine();
        tgEngine.RunAsync(ignoreMessagesEvents: true).Wait();
    }

    //[Fact]
    //public async Task GetAndExtractFactsFullTest() {
    //    long chatId = -1001705017627;
    //    var sinceDate = DateTime.Now.AddMinutes(-10);

    //    //var tdMessages = await tgEngine.GetTdMessages(chatId, sinceDate);
    //    //List<TgMessage> soanxMessages = tgEngine.ConvertToSoanxMessages(tdMessages);
    //    //await tgEngine.SaveTgMessages(soanxMessages);

    //    var soanxDbMessages = await tgEngine.LoadTgMessagesFromDbAsync(chatId, sinceDate);
    //    tgEngine.FormilizeMessages(soanxDbMessages);
    //}


    [Fact]
    public async Task ExtractFactsWIthExamplesTest() {
        var openAiParameters = config.GetRequiredSection("OpenAiSettings").Get<OpenAiSettings>();
        HttpClient httpClient = new HttpClient() { Timeout = new TimeSpan(0, 5, 0) };

        OpenAIService openAiService = new OpenAIService(
            new OpenAiOptions() {
                ApiKey = openAiParameters.OpenAiApiKey
            },
            httpClient
        );
        
        var promptHelper = new ChatPromptHelper();
        await promptHelper.InitializePromptCollections("MontenegroExchange");

        var request = new ChatCompletionCreateRequest {
            Messages = new List<ChatMessage>
            {
                ChatMessage.FromUser(promptHelper.Instruction),
                ChatMessage.FromUser(promptHelper.ResultSchemaJson),
                ChatMessage.FromUser(promptHelper.MessagesJson),
                ChatMessage.FromAssistant(promptHelper.FormalizedMessagesJson),
                
                ChatMessage.FromUser($"[{{\"Id\": 123,\"Message\": \"📍Бар.\\r\\n-Выдадим наличные EUR за Ваши:\\r\\nБезналичные рубли 92 - 92.7\\r\\nБезналичные гривны 43 - 44\\r\\nНаличные доллары 0,89 - 0.9\\r\\n-Купим Вашу криптовалюту:\\r\\nUSDT/USDC/BUSD 0,895 - 0,905\"}}]"),
                //ChatMessage.FromUser("На основании предыдущих заданий верни один элемент массива json, который содержит все возможные поля."),
            },
            Model = OpenAI.ObjectModels.Models.ChatGpt3_5Turbo
        };

        try {
            var completionResult = await openAiService.ChatCompletion.CreateCompletion(request);
            if (completionResult.Successful) {
                Console.WriteLine(completionResult.Choices.First().Message.Content);
            }
            else {
                if (completionResult.Error == null) {
                    throw new Exception("Unknown Error");
                }
            }
        } catch (Exception ex) {
        }
    }

    [Fact]
    public async Task ValidateOpenAiTest() {
        var openAiParameters = config.GetRequiredSection("OpenAiSettings").Get<OpenAiSettings>();
        OpenAIService openAiService = new OpenAIService(new OpenAiOptions() {
            ApiKey = openAiParameters.OpenAiApiKey
        });

        var services = await openAiService.ListModel();

        var completionRequest = new CompletionCreateRequest() {
            Prompt = "Once upon a time",
            Model = OpenAI.ObjectModels.Models.TextDavinciV3
        };
        var completionResult = await openAiService.Completions.CreateCompletion(completionRequest);
    }
}
