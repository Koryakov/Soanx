using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels.SharedModels;
using Serilog;
using Soanx.CurrencyExchange;
using Soanx.CurrencyExchange.EfModels;
using Soanx.CurrencyExchange.OpenAiDtoModels;

namespace Soanx.TelegramAnalyzer;
public class OpenAiChoicesConvertor {
    private Serilog.ILogger log = Log.ForContext<OpenAiChoicesConvertor>();
    public List<FormalizedMessage> ConvertToFormalized(ICollection<ChatChoiceResponse> chatResponseList) {
        var locLog = log.ForContext("method", "ConvertToFormalized()");
        locLog.Verbose("IN.");
        var result = new List<FormalizedMessage>();
        //TODO: check does chatChoiceResponse is json array or single json object?
        foreach (var chatChoiceResponse in chatResponseList) {
            try {
                ChatMessage message = chatChoiceResponse.Message;
                var formilizedMessage = SerializationHelper.DeserializeJson<FormalizedMessage>(message.Content);
                result.Add(formilizedMessage);
            } catch (Exception ex) {
                locLog.Error<ChatChoiceResponse>(ex, "Error due to ChatChoiceResponse message converting. chatChoiceResponse: {@chatChoiceResponse}", chatChoiceResponse);
            }
        }
        locLog.Verbose("OUT.");
        return result;
    }
}