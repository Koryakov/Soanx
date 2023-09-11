using OpenAI.ObjectModels.SharedModels;
using Serilog;
using Soanx.CurrencyExchange;
using Soanx.CurrencyExchange.OpenAiDtoModels;
using Soanx.Repositories;
using Soanx.Repositories.Models;
using Soanx.TelegramAnalyzer.Models;
using System.Collections.Concurrent;

namespace Soanx.TelegramAnalyzer;
public class CurrencyAnalyzingWorker : ITelegramWorker {

    private Serilog.ILogger log = Log.ForContext<CurrencyAnalyzingWorker>();
    private CancellationToken cancellationToken;
    //private ManualResetEvent newTgMessagesEvent = new(false);
    private ConcurrentBag<MessageForAnalyzing> messagesForAnalyzing = new();
    private ConcurrentBag<ChatChoiceResponse> analyzedForStoring = new();
    private TgRepository tgRepository;
    private List<Task> tasks = new List<Task>();

    public OpenAiSettings OpenAiSettings { get; private set; }
    public TgCurrencyAnalyzingSettings TgCurrencyExtractorSettings { get; private set; }
    public int ReadingBatchSize { get; private set; } = 10;
    public int AnalyzingBatchSize { get; private set; } = 10;
    public int SavingBatchSize { get; private set; } = 10;
    public int ReadIntervalSec { get; private set; } = 5;
    public int AnalyzeIntervalSec { get; private set; } = 5;

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
        tasks.Add(Task.Run(() => Store()));

        await Task.WhenAll(tasks);
        locLog.Information("OUT");
    }

    private async Task Read() {
        var locLog = log.ForContext("method", "Read()");
        locLog.Information("IN");

        while (!cancellationToken.IsCancellationRequested) {
            try {
                var batchForAnalyzing = await tgRepository
                    .GetTgMessagesByAnalyzedStatus(
                        TgMessage.TgMessageAnalyzedStatus.Unknown, TgMessage.TgMessageAnalyzedStatus.InProcess, ReadingBatchSize);
                foreach (MessageForAnalyzing msg in batchForAnalyzing) {
                    messagesForAnalyzing.Add(msg);
                }
                if (batchForAnalyzing.Count > 0) {
                    locLog.Information("{@analyzingCount} messages read from db and added into messagesForAnalyzing collection.", messagesForAnalyzing.Count);
                }
                Task.Delay(ReadIntervalSec * 1000).Wait();
            } catch (Exception ex) {
                locLog.Error(ex, "Method processing will continue after pause.");
                Task.Delay(5000);
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
                    List<MessageForAnalyzing> messagesList = TakeMessagesBatch();

                    if (messagesList.Count > 0) {
                        locLog.Information("msg in collectionForAnalyzing = {@allCollection}, taken to save = {@takenCount}", messagesForAnalyzing.Count, messagesList.Count);
                        var result = await openAiApiClient.SendOpenAiRequest(messagesList);
                        if (result.IsSuccess) {
                            foreach (var choice in result.Choices) {
                                analyzedForStoring.Add(choice);
                            }
                        }
                        else {
                            //TODO: Negative scenario should be implemented here
                        }
                    }
                    Task.Delay(AnalyzeIntervalSec * 1000).Wait();
                } catch (Exception ex) {
                    locLog.Error(ex, "Method processing will continue after pause.");
                    Task.Delay(5000);
                }
            }
        } catch (Exception ex) {
            locLog.Error(ex, "Method will be finished.");
        }

        locLog.Information("OUT");
    }

    private List<MessageForAnalyzing> TakeMessagesBatch() {
        var locLog = log.ForContext("method", "TakeMessagesBatch()");

        var messagesList = new List<MessageForAnalyzing>(AnalyzingBatchSize);

        for (int i = 0; i < Math.Min(AnalyzingBatchSize, messagesForAnalyzing.Count); i++) {
            if (messagesForAnalyzing.TryTake(out var item)) {
                messagesList.Add(item);
            }
        }
        locLog.Verbose("{@takenCount} messages was taken.", messagesList.Count);

        return messagesList;
    }

    private async Task Store() {
        var locLog = log.ForContext("method", "Store()");
        locLog.Information("IN");

        while (!cancellationToken.IsCancellationRequested) {
            var analyzedBatchList = new List<ChatChoiceResponse>(SavingBatchSize);

            for (int i = 0; i < Math.Min(AnalyzingBatchSize, analyzedBatchList.Count); i++) {
                if (analyzedForStoring.TryTake(out var item)) {
                    analyzedBatchList.Add(item);
                }
            }

            if (analyzedBatchList.Count > 0) {
                locLog.Information("analyzedBatchList.Count = {@analyzedCount}", analyzedBatchList.Count);
                try {
                    var convertor = new OpenAiChoicesConvertor();
                    //TODO: here should be converting analyzing results to EF models

                    //List<CurrencyExchange.EfModels.FormalizedMessage> formalizedList = convertor.ConvertToFormalized(analyzedBatchList);
                }
                catch (Exception ex) {
                    foreach (var analyzedMessage in analyzedBatchList) {
                        analyzedForStoring.Add(analyzedMessage);
                    }
                    locLog.Error(ex, "messages have been returned to the analyzedForStoring.");
                }
            }
        }
        locLog.Information("OUT");
    }
}