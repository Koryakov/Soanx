using OpenAI.ObjectModels.RequestModels;
using Serilog;
using Soanx.CurrencyExchange.Models;

namespace Soanx.CurrencyExchange;
public class ChatPromptHelper {
    private Serilog.ILogger log = Log.ForContext<ChatPromptHelper>();
    public string CorpusName { get; private set; }
    public string Instruction {  get; private set; }
    public string ResultSchemaJson {  get; private set; }
    public string MessagesJson {  get; private set; }
    public string FormalizedMessagesJson {  get; private set; }
    public List<ChatMessage> PromptingSetList { get; private set; }

    private List<DtoModels.FormalizedMessageEx> messagesExList;
    private List<DtoModels.FormalizedMessage> messagesList;

    public static async Task<ChatPromptHelper> CreateNew(string corpusName) {
        var helper = new ChatPromptHelper();
        await helper.Initialize(corpusName);
        return helper;
    }

    private async Task Initialize(string corpusName) {
        var locLog = log.ForContext("method", "Initialize");
        locLog.Information("IN. corpusName = {@corpusName}", corpusName);

        CorpusName = corpusName;
        string relatedPath = Path.Combine("OpenAICorpus", corpusName);
        Instruction = await File.ReadAllTextAsync(Path.Combine(relatedPath, "Instruction.txt"));

        using FileStream promptSchema = File.OpenRead(Path.Combine(relatedPath, "ResultSchema.json"));
        var schemaJsonObj = await SerializationHelper.DeserializeJsonAsync<object>(promptSchema);
        ResultSchemaJson = SerializationHelper.Serialize<object>(schemaJsonObj);

        using FileStream promptJson = File.OpenRead(Path.Combine(relatedPath, "Examples.json"));

        messagesExList = await SerializationHelper.DeserializeJsonAsync<List<DtoModels.FormalizedMessageEx>>(promptJson);
        messagesList = messagesExList.ConvertAll<DtoModels.FormalizedMessage>(m => (DtoModels.FormalizedMessage)m);

        MessagesJson = SerializationHelper.Serialize<IEnumerable<DtoModels.MessageToAnalyzing>>(
            messagesExList.Select(ex => ex.Message));

        FormalizedMessagesJson = SerializationHelper.Serialize(messagesList);

        InitializePromptsCollection();
        locLog.Information("Instruction: {@Instruction}", Instruction);
        locLog.Information("ResultSchemaJson: {@ResultSchemaJson}", ResultSchemaJson);
        locLog.Information("MessagesJson: {@MessagesJson}", MessagesJson);
        locLog.Information("FormalizedMessagesJson: {@FormalizedMessagesJson}", FormalizedMessagesJson);
        locLog.Information("OUT.");
    }

    private void InitializePromptsCollection() {
        PromptingSetList = new List<ChatMessage>() {
        //ChatMessage.FromSystem(Instruction),
        //ChatMessage.FromSystem(ResultSchemaJson),
        ChatMessage.FromUser(Instruction),
        ChatMessage.FromUser(ResultSchemaJson),

        ChatMessage.FromUser(MessagesJson),
        ChatMessage.FromAssistant(FormalizedMessagesJson),
        };
    }
}
