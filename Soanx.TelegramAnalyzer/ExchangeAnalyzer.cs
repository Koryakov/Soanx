using Serilog;
using Soanx.CurrencyExchange;
using Soanx.TelegramAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Soanx.CurrencyExchange.Models.DtoModels;
using static System.Collections.Specialized.BitVector32;

namespace Soanx.TelegramAnalyzer;
public class ExchangeAnalyzer {

    private Serilog.ILogger log = Log.ForContext<ExchangeAnalyzer>();
    
    private readonly List<MessageToAnalyzing> messageBuffer = new();
    private OpenAiSettings openAiSettings;

    private SoanxQueue<MessageToAnalyzing> analysisQueue;
    private SoanxQueue<FormalizedMessage> formalizedQueue;
    private SoanxQueue<FormalizedMessage> unmatchedQueue;

    public ExchangeAnalyzer(OpenAiSettings openAiSettings, 
        QueueMessagingSettings queueMessagingSettings, RabbitMqConnection rabbitMqConnection) {

        this.openAiSettings = openAiSettings;
        analysisQueue = new SoanxQueue<MessageToAnalyzing>(rabbitMqConnection, queueMessagingSettings.MessagesToAnalyzeSettings);
        formalizedQueue = new SoanxQueue<FormalizedMessage>(rabbitMqConnection, queueMessagingSettings.FormalizedMessagesSettings);
        unmatchedQueue = new SoanxQueue<FormalizedMessage>(rabbitMqConnection, queueMessagingSettings.NotMatchedMessagesSettings);
    }

    public void AnalyzeTask() {
        analysisQueue.Subscribe(OnMessageReceived);
    }

    private bool OnMessageReceived(MessageToAnalyzing message, ulong deliveryTag) {
        lock (messageBuffer) {
            messageBuffer.Add(message);

            //TODO: CHANGE count to tokens!
            if (messageBuffer.Count >= 5) {
                // Extract the current batch for processing and clear the buffer
                var batchToProcess = messageBuffer.ToList();
                messageBuffer.Clear();

                // Process the batch in parallel
                Task.Run(() => Formalize(batchToProcess));
                return true;  // Return true if you consider this as a successful receipt of the message
            }
        }
        return false; // Return false if you consider this as a failed receipt and want to requeue the message
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
                    foreach (var formalizedMessage in convertingResult.formalizedMessages!) {
                        if (formalizedMessage.NotMatched == true) {
                            locLog.Debug<FormalizedMessage>("Not Matched FormalizedMessage was found. {@notMatchedMessage}", formalizedMessage);
                            unmatchedQueue.Send(formalizedMessage);
                        }
                        else {
                            formalizedQueue.Send(formalizedMessage);
                        }
                    }
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
}