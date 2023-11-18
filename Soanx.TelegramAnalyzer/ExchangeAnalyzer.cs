using Serilog;
using Soanx.CurrencyExchange;
using Soanx.CurrencyExchange.Models;
using Soanx.Repositories;
using Soanx.TelegramAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Soanx.CurrencyExchange.Models.DtoModels;
using static System.Collections.Specialized.BitVector32;

namespace Soanx.TelegramAnalyzer;
public class ExchangeAnalyzer {

    private Serilog.ILogger log = Log.ForContext<ExchangeAnalyzer>();
    
    private OpenAiSettings openAiSettings;
    private TgRepository tgRepository;
    private Cache cache;

    private SoanxQueue<List<MessageToAnalyzing>> analysisQueue;
    private SoanxQueue<FormalizedMessage> formalizedQueue;
    private SoanxQueue<FormalizedMessage> unmatchedQueue;

    public ExchangeAnalyzer(OpenAiSettings openAiSettings, QueueMessagingSettings queueMessagingSettings, 
        RabbitMqConnection rabbitMqConnection, string soanxConnectionString, CacheSettings cacheSettings) {

        this.openAiSettings = openAiSettings;
        analysisQueue = new SoanxQueue<List<MessageToAnalyzing>>(rabbitMqConnection, queueMessagingSettings.MessagesToAnalyzeSettings);
        formalizedQueue = new SoanxQueue<FormalizedMessage>(rabbitMqConnection, queueMessagingSettings.FormalizedMessagesSettings);
        unmatchedQueue = new SoanxQueue<FormalizedMessage>(rabbitMqConnection, queueMessagingSettings.NotMatchedMessagesSettings);
        tgRepository = new TgRepository(soanxConnectionString);
        cache = new Cache(cacheSettings, soanxConnectionString);
    }

    public void SubscribeToQueueForAnalyzing() {
        //TODO; modify it to prevent multiple subscriptions
        analysisQueue.Subscribe(OnMessageReceived);
    }

    private bool OnMessageReceived(List<MessageToAnalyzing> messages, ulong deliveryTag) {
        var locLog = log.ForContext("method", "OnMessageReceived()");
        //locLog.Information("message id: {@id}", message.Id);

        //TODO: CHANGE count to tokens!
        Task.Run(() => Formalize(messages));

        return true;
    }

    private async Task Formalize(List<MessageToAnalyzing> messages) {
        var locLog = log.ForContext("method", "Formalize()");
        locLog.Debug("IN, messages count = {@msgCount}", messages.Count);
        bool result = false;
        try {
            var mneExchangePromptHelper = await ChatPromptHelper.CreateNew("MontenegroExchange");
            //TODO: Gtp model name should be moved to appsettings
            var openAiApiClient = new OpenAiApiClient(
                openAiSettings.OpenAiApiKey, mneExchangePromptHelper, OpenAI.ObjectModels.Models.Gpt_4);

            //TODO: Check for context_length_exceeded must be done here
            var chatChoiceResultList = await openAiApiClient.SendOpenAiRequest(messages);

            if (chatChoiceResultList.IsSuccess) {
                var convertingResult = OpenAiChoicesConvertor.ConvertToFormalized(chatChoiceResultList.Choices);
                if (convertingResult.isSuccess) {
                    var formalizedToSave = new List<FormalizedMessage>();
                    foreach (var formalizedMessage in convertingResult.formalizedMessages!) {
                        if (formalizedMessage.NotMatched == true) {
                            locLog.Debug<FormalizedMessage>("Not Matched FormalizedMessage was found. {@notMatchedMessage}", formalizedMessage);
                            unmatchedQueue.Send(formalizedMessage);
                        }
                        else {
                            //formalizedQueue.Send(formalizedMessage);
                            formalizedToSave.Add(formalizedMessage);
                        }
                    }
                    await Save(formalizedToSave);
                }
                else {
                    //formalizedMessages returned from OpenAI are not in consistent state.
                    //Usually it's result of wrong OpenAI behavior. So we need to formalize it again
                    //ReturnMessagesToAnalyzeQueue(messages);
                    result = false;
                    locLog.Warning("Incorrectly formalized messages were returned to collection to formalize again.");
                }
            }
            else {
                result = false;
                //ReturnMessagesToAnalyzeQueue(messages);
            }
        }
        catch (Exception ex) {
            result = false;
            locLog.Error(ex, "Method will be finished.");
        } finally {
            locLog.Information("OUT, result = {@result}", result);
        }
        
        //void ReturnMessagesToAnalyzeQueue(List<MessageToAnalyzing> messages) {
        //    //TODO: Add a counter to track the number of attempts for each message
        //    foreach (var message in messages) {
        //        queueDispatcher.AnalysisQueueHelper.Send(message);
        //    }
        //}
    }

    private async Task Save(List<DtoModels.FormalizedMessage> formalizedMessages) {
        var locLog = log.ForContext("method", "Save()");
        locLog.Information("IN");

        try {
            var cityDictionary = await cache.GetCityDictionary();
            //TODO: Here, formalized messages should be converted to EF entities and saved to db
            //TODO: In the future conversion should be performed as soon as new formalized message is created.
            List<EfModels.ExchangeOffer> exchangeOfferList = DtoToEfModelsConvertor.ConvertToExchangeOffers(formalizedMessages, cityDictionary);
            await tgRepository.SaveExchangeOffers(exchangeOfferList);
        }
        catch (Exception ex) {
            locLog.Error(ex, "Error due to saving formalized messages");
            await Task.Delay(5000);
        }

        locLog.Information("OUT");
    }
}