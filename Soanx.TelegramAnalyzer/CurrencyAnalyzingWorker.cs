﻿using OpenAI.ObjectModels.SharedModels;
using Serilog;
using Soanx.CurrencyExchange;
using Soanx.CurrencyExchange.Models;
using Soanx.Repositories;
using Soanx.Repositories.Models;
using Soanx.TelegramAnalyzer.Models;
using System.Collections.Concurrent;
using Telegram.Bot.Types;

namespace Soanx.TelegramAnalyzer;
public class CurrencyAnalyzingWorker : ITelegramWorker {

    public class LoopSettings {
        public int BatchSize { get; set; }
        public int IntervalSeconds { get; set; }
    }

    private Serilog.ILogger log = Log.ForContext<CurrencyAnalyzingWorker>();
    private CancellationToken cancellationToken;
    //private ManualResetEvent newTgMessagesEvent = new(false);
    private ConcurrentBag<DtoModels.MessageForAnalyzing> messagesForAnalyzing = new();
    private ConcurrentBag<DtoModels.FormalizedMessage> formalizedMessages = new();
    private TgRepository tgRepository;
    private Cache cache;
    private List<Task> tasks = new List<Task>();

    public OpenAiSettings OpenAiSettings { get; private set; }
    public TgCurrencyAnalyzingSettings TgCurrencyExtractorSettings { get; private set; }

    private LoopSettings ReadTgMessagesSettings = new() { BatchSize = 3, IntervalSeconds = 5 };
    private LoopSettings AnalyzeSettings = new() { BatchSize = 3, IntervalSeconds = 15 };
    private LoopSettings SaveFormalizedSettings = new() { BatchSize = 3, IntervalSeconds = 5 };

    /*
    Steps of processing data: 
    1) Saving to db (count or time?)
    2) Reading from db to analyze (count)
    3) Saving formalized to db (count)

    */
    public CurrencyAnalyzingWorker(OpenAiSettings openAiSettings, TgCurrencyAnalyzingSettings tgCurrencyExtractorSettings,
        string soanxConnectionString, CacheSettings cacheSettings) {

        tgRepository = new TgRepository(soanxConnectionString);
        OpenAiSettings = openAiSettings;
        TgCurrencyExtractorSettings = tgCurrencyExtractorSettings;
        cache = new Cache(cacheSettings, soanxConnectionString);
    }

    public async Task Run(CancellationToken cancellationToken) {
        this.cancellationToken = cancellationToken;
        var locLog = log.ForContext("method", "Run()");
        locLog.Information("IN");

        tasks.Add(Task.Run(() => Read()));
        tasks.Add(Task.Run(() => Analyze()));
        tasks.Add(Task.Run(() => Save()));

        await Task.WhenAll(tasks);
        locLog.Information("OUT");
    }

    private async Task Read() {
        var locLog = log.ForContext("method", "Read()");
        locLog.Information("IN");

        while (!cancellationToken.IsCancellationRequested) {
            try {
                var result = await tgRepository
                    .GetTgMessagesByAnalyzedStatus(minReturningCount: ReadTgMessagesSettings.BatchSize,
                        TgMessage.TgMessageAnalyzedStatus.Unknown, TgMessage.TgMessageAnalyzedStatus.InProcess);

                if (result.isSuccess) {
                    foreach(DtoModels.MessageForAnalyzing msg in result.messages!) {
                        messagesForAnalyzing.Add(msg);
                    }
                    locLog.Information("{@analyzingCount} messages read from db and added into messagesForAnalyzing collection.", messagesForAnalyzing.Count);
                }
                await Task.Delay(ReadTgMessagesSettings.IntervalSeconds * 1000);
            } catch (Exception ex) {
                locLog.Error(ex, "Method processing will continue after pause.");
                await Task.Delay(5000);
            }
        }
        locLog.Information("OUT");
    }
    private async Task Analyze() {
        var locLog = log.ForContext("method", "Analyze()");
        locLog.Information("IN");

        try {
            var mneExchangePromptHelper = await ChatPromptHelper.CreateNew("MontenegroExchange");

            var openAiApiClient = new OpenAiApiClient(
                OpenAiSettings.OpenAiApiKey, mneExchangePromptHelper, OpenAI.ObjectModels.Models.ChatGpt3_5Turbo);

            while (!cancellationToken.IsCancellationRequested) {
                try {
                    if (messagesForAnalyzing.Count >= AnalyzeSettings.BatchSize) {
                        List<DtoModels.MessageForAnalyzing> messagesList = TakeMessagesBatchForAnalyzing(AnalyzeSettings.BatchSize);

                        if (messagesList.Count > 0) {
                            locLog.Information("msg in collectionForAnalyzing = {@allCollection}, taken to save = {@takenCount}", messagesForAnalyzing.Count, messagesList.Count);
                            //TODO: Check for context_length_exceeded must be done here
                            var chatChoiceResultList = await openAiApiClient.SendOpenAiRequest(messagesList);
                            
                            if (chatChoiceResultList.IsSuccess) {
                                var result = OpenAiChoicesConvertor.ConvertToFormalized(chatChoiceResultList.Choices);
                                if (result.isSuccess) {
                                    foreach (var formalizedMessage in result.formalizedMessages!) {
                                        formalizedMessages.Add(formalizedMessage);
                                    }
                                } else {
                                    //formalizedMessages returned from OpenAI are not in consistent state.
                                    //Usually it's result of wrong OpenAI behavior. So we need to formalize it again
                                    foreach (var message in messagesList) {
                                        messagesForAnalyzing.Add(message);
                                    }
                                    locLog.Warning("Incorrectly formalized messages were returned to collection to formalize again.");
                                }
                            }
                            else {
                                //TODO: Negative scenario should be implemented here
                            }
                        }
                    }
                    await Task.Delay(AnalyzeSettings.IntervalSeconds * 1000);
                } catch (Exception ex) {
                    locLog.Error(ex, "Method processing will continue after pause.");
                    await Task.Delay(5000);
                }
            }
        } catch (Exception ex) {
            locLog.Error(ex, "Method will be finished.");
        }
        locLog.Information("OUT");

        
        List<DtoModels.MessageForAnalyzing> TakeMessagesBatchForAnalyzing(int batchSize) {
            var locLog = log.ForContext("method", "TakeMessagesBatchForAnalyzing()");

            var messagesList = new List<DtoModels.MessageForAnalyzing>(batchSize);
            int count = Math.Min(batchSize, messagesForAnalyzing.Count);
            for (int i = 0; i < count; i++) {
                if (messagesForAnalyzing.TryTake(out var item)) {
                    messagesList.Add(item);
                }
            }
            locLog.Verbose("{@takenCount} messages was taken.", messagesList.Count);

            return messagesList;
        }
    }

    private async Task Save() {
        var locLog = log.ForContext("method", "Save()");
        locLog.Information("IN");

        int batchSize = SaveFormalizedSettings.BatchSize;
        while (!cancellationToken.IsCancellationRequested) {
            var batchToSave = new List<DtoModels.FormalizedMessage>(batchSize);
            try {
                if (formalizedMessages.Count >= batchSize) {
                    for (int i = 0; i < batchSize; i++) {
                        if (formalizedMessages.TryTake(out var item)) {
                            batchToSave.Add(item);
                        }
                    }
                    locLog.Information("analyzedBatchList.Count = {@analyzedCount}", batchToSave.Count);
                    //TODO: Here, formalized messages should be converted to EF entities and saved to db
                    //TODO: In the future conversion should be performed as soon as new formalized message is created.
                    List<EfModels.ExchangeOffer> exchangeOfferList = 
                        ModelsConvertor.ConvertDtoToEf(batchToSave, await cache.GetCityDictionary());
                    tgRepository.AddExchangeOffers(exchangeOfferList);
                }
                await Task.Delay(SaveFormalizedSettings.IntervalSeconds * 1000);
            }
            catch (Exception ex) {
                foreach (var analyzedMessage in batchToSave) {
                    formalizedMessages.Add(analyzedMessage);
                }
                locLog.Error(ex, "messages have been returned to the analyzedForStoring.");
                await Task.Delay(5000);
            }

        }
        locLog.Information("OUT");
    }

}