using OpenAI.ObjectModels.SharedModels;
using Serilog;
using Soanx.CurrencyExchange;
using Soanx.CurrencyExchange.Models;
using Soanx.Repositories;
using Soanx.Repositories.Models;
using Soanx.TelegramAnalyzer.Models;
using System.Collections.Concurrent;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static Soanx.CurrencyExchange.Models.DtoModels;

namespace Soanx.TelegramAnalyzer;
public class CurrencyAnalyzingWorker : ITelegramWorker {

    public class LoopSettings {
        public int BatchSize { get; set; }
        public int IntervalSeconds { get; set; }
    }
    public OpenAiSettings OpenAiSettings { get; private set; }
    public TgCurrencyAnalyzingSettings TgCurrencyExtractorSettings { get; private set; }

    private Serilog.ILogger log = Log.ForContext<CurrencyAnalyzingWorker>();
    private CancellationToken cancellationToken;
    //private ManualResetEvent newTgMessagesEvent = new(false);
    private ConcurrentBag<DtoModels.MessageToAnalyzing> messagesForAnalyzing = new();
    private ConcurrentBag<DtoModels.FormalizedMessage> formalizedMessages = new();
    private ConcurrentBag<DtoModels.FormalizedMessage> notMatchedMessages = new();
    private TgRepository tgRepository;
    private Cache cache;
    private List<Task> tasks = new List<Task>();

    private SoanxQueue<MessageToAnalyzing> analysisQueue;

    private LoopSettings ReadTgMessagesSettings = new() { BatchSize = 1, IntervalSeconds = 5 };
    private LoopSettings AnalyzeSettings = new() { BatchSize = 1, IntervalSeconds = 15 };
    private LoopSettings SaveFormalizedSettings = new() { BatchSize = 1, IntervalSeconds = 5 };

    private ExchangeAnalyzer exchangeAnalyzer;

    /*
    Steps of processing data: 
    1) Saving to db (count or time?)
    2) Reading from db to analyze (count)
    3) Saving formalized to db (count)

    */
    public CurrencyAnalyzingWorker(OpenAiSettings openAiSettings, TgCurrencyAnalyzingSettings tgCurrencyExtractorSettings,
        string soanxConnectionString, CacheSettings cacheSettings, QueueConfigurations queueConfiguration) {

        tgRepository = new TgRepository(soanxConnectionString);
        OpenAiSettings = openAiSettings;
        TgCurrencyExtractorSettings = tgCurrencyExtractorSettings;
        cache = new Cache(cacheSettings, soanxConnectionString);

        var rabbitMqConnection = new RabbitMqConnection(queueConfiguration.RabbitMqCredentials);

        analysisQueue = new SoanxQueue<MessageToAnalyzing>(
            rabbitMqConnection, queueConfiguration.QueueMessagingSettings.MessagesToAnalyzeSettings);
        
        exchangeAnalyzer = new ExchangeAnalyzer(openAiSettings, queueConfiguration.QueueMessagingSettings, rabbitMqConnection);
    }

    public async Task Run(CancellationToken cancellationToken) {
        this.cancellationToken = cancellationToken;
        var locLog = log.ForContext("method", "Run()");
        locLog.Information("IN");

        tasks.Add(Task.Run(() => Read()));
        //tasks.Add(Task.Run(() => Analyze()));
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
                    foreach (DtoModels.MessageToAnalyzing msg in result.messages!) {
                        analysisQueue.Send(msg);
                        exchangeAnalyzer.AnalyzeTask();
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
                    var cityDictionary = await cache.GetCityDictionary();
                    //TODO: Here, formalized messages should be converted to EF entities and saved to db
                    //TODO: In the future conversion should be performed as soon as new formalized message is created.
                    List<EfModels.ExchangeOffer> exchangeOfferList = DtoToEfModelsConvertor.ConvertToExchangeOffers(batchToSave, cityDictionary);
                    await tgRepository.SaveExchangeOffers(exchangeOfferList);
                }
                await Task.Delay(SaveFormalizedSettings.IntervalSeconds * 1000);
            }
            catch (Exception ex) {
                locLog.Error(ex, "Error due to saving formalized messages");
                await Task.Delay(5000);
            }

        }
        locLog.Information("OUT");
    }

}