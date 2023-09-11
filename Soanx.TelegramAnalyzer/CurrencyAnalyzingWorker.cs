using OpenAI.ObjectModels.SharedModels;
using Serilog;
using Soanx.CurrencyExchange;
using Soanx.CurrencyExchange.OpenAiDtoModels;
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
    private ConcurrentBag<MessageForAnalyzing> messagesForAnalyzing = new();
    private ConcurrentBag<FormalizedMessage> formalizedMessages = new();
    private TgRepository tgRepository;
    private List<Task> tasks = new List<Task>();

    public OpenAiSettings OpenAiSettings { get; private set; }
    public TgCurrencyAnalyzingSettings TgCurrencyExtractorSettings { get; private set; }

    private LoopSettings ReadTgMessagesSettings = new LoopSettings() { BatchSize = 3, IntervalSeconds = 5 };
    private LoopSettings AnalyzeSettings = new LoopSettings() { BatchSize = 3, IntervalSeconds = 5 };
    private LoopSettings SaveFormalizedSettings = new LoopSettings() { BatchSize = 1, IntervalSeconds = 5 };

    /*
    Steps of processing data: 
    1) Saving to db (count or time?)
    2) Reading from db to analyze (count)
    3) Saving formalized to db (count)

    */
    public CurrencyAnalyzingWorker(OpenAiSettings openAiSettings,
        TgCurrencyAnalyzingSettings tgCurrencyExtractorSettings, string soanxConnectionString) {

        tgRepository = new TgRepository(soanxConnectionString);
        OpenAiSettings = openAiSettings;
        TgCurrencyExtractorSettings = tgCurrencyExtractorSettings;
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
                    foreach (MessageForAnalyzing msg in result.messages!) {
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
                        List<MessageForAnalyzing> messagesList = TakeMessagesBatch(AnalyzeSettings.BatchSize);

                        if (messagesList.Count > 0) {
                            locLog.Information("msg in collectionForAnalyzing = {@allCollection}, taken to save = {@takenCount}", messagesForAnalyzing.Count, messagesList.Count);
                            var result = await openAiApiClient.SendOpenAiRequest(messagesList);
                            if (result.IsSuccess) {
                                List<FormalizedMessage> convertedList = OpenAiChoicesConvertor.ConvertToFormalized(result.Choices);
                                foreach (var formalizedMessage in convertedList) {
                                    formalizedMessages.Add(formalizedMessage);
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

        
        List<MessageForAnalyzing> TakeMessagesBatch(int batchSize) {
            var locLog = log.ForContext("method", "TakeMessagesBatch()");

            var messagesList = new List<MessageForAnalyzing>(batchSize);

            for (int i = 0; i < Math.Min(batchSize, messagesForAnalyzing.Count); i++) {
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
            var batchToSave = new List<FormalizedMessage>(batchSize);
            try {
                if (formalizedMessages.Count >= batchSize) {
                    
                    for (int i = 0; i < Math.Min(batchSize, formalizedMessages.Count); i++) {
                        if (formalizedMessages.TryTake(out var item)) {
                            batchToSave.Add(item);
                        }
                    }
                    locLog.Information("analyzedBatchList.Count = {@analyzedCount}", batchToSave.Count);
                    //TODO: here should be converting formalized results to EF models and store to db

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