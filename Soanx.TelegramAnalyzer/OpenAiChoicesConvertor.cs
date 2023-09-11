using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels.SharedModels;
using Serilog;
using Soanx.CurrencyExchange;
using Soanx.CurrencyExchange.EfModels;
using Soanx.CurrencyExchange.OpenAiDtoModels;

namespace Soanx.TelegramAnalyzer;
public class OpenAiChoicesConvertor {
    private static Serilog.ILogger log = Log.ForContext<OpenAiChoicesConvertor>();
    public static List<FormalizedMessage> ConvertToFormalized(ICollection<ChatChoiceResponse> formalizedChoiceList) {
        var locLog = log.ForContext("method", "ConvertToFormalized()");
        locLog.Verbose("IN.");
        var result = new List<FormalizedMessage>();
        //TODO: check does chatChoiceResponse is json array or single json object?
        foreach (var chatChoiceResponse in formalizedChoiceList) {
            try {
                ChatMessage message = chatChoiceResponse.Message;
                var convertedFormalizedList = SerializationHelper.DeserializeJson<List<FormalizedMessage>>(message.Content);
                result.AddRange(convertedFormalizedList);
            } catch (Exception ex) {
                locLog.Error<ChatChoiceResponse>(ex, "Error due to ChatChoiceResponse message converting. chatChoiceResponse: {@chatChoiceResponse}", chatChoiceResponse);
            }
        }
        locLog.Verbose("OUT.");
        return result;
    }
}