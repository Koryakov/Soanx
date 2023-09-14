using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels.SharedModels;
using Serilog;
using Soanx.CurrencyExchange;
using Soanx.CurrencyExchange.Models;
using System.Text.Json;

namespace Soanx.TelegramAnalyzer;
public class OpenAiChoicesConvertor {
    private static Serilog.ILogger log = Log.ForContext<OpenAiChoicesConvertor>();

    public static (bool isSuccess, List<DtoModels.FormalizedMessage>? formalizedMessages)
        ConvertToFormalized(ICollection<ChatChoiceResponse> formalizedChoiceList) {

        var locLog = log.ForContext("method", "ConvertToFormalized()");
        locLog.Verbose("IN.");
        var formalizedMessages = new List<DtoModels.FormalizedMessage>();
        foreach (var chatChoiceResponse in formalizedChoiceList) {
            try {
                ChatMessage message = chatChoiceResponse.Message;
                var convertedFormalizedList = SerializationHelper.DeserializeJson<List<DtoModels.FormalizedMessage>>(message.Content);
                formalizedMessages.AddRange(convertedFormalizedList);

            } catch (JsonException jsonEx) {
                locLog.Error<ChatChoiceResponse>(jsonEx, "Error due to ChatChoiceResponse message converting. chatChoiceResponse: {@chatChoiceResponse}", chatChoiceResponse);
                return (false, null);
            }
        }
        locLog.Verbose("OUT.");
        return (true, formalizedMessages);
    }
}