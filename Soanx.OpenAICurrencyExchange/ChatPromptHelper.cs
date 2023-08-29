using Soanx.CurrencyExchange.OpenAiDtoModels;

namespace Soanx.CurrencyExchange;
public class ChatPromptHelper {
    public string CorpusName { get; private set; }
    public string Instruction {  get; private set; }
    public string ResultSchemaJson {  get; private set; }
    public string MessagesJson {  get; private set; }
    public string FormalizedMessagesJson {  get; private set; }
    public List<PromptingSet> PromptingSetList { get { return promptingSetList; } }

    private List<FormalizedMessageEx> messagesExList;
    private List<FormalizedMessage> messagesList;
    private List<PromptingSet> promptingSetList;

    public static async Task<ChatPromptHelper> CreateNew(string corpusName) {
        var helper = new ChatPromptHelper();
        await helper.InitializePromptCollections(corpusName);
        return helper;
    }

    public async Task InitializePromptCollections(string corpusName) {
        CorpusName = corpusName;

        string relatedPath = Path.Combine("OpenAICorpus", corpusName);
        Instruction = await File.ReadAllTextAsync(Path.Combine(relatedPath, "Instruction.txt"));

        using FileStream promptSchema = File.OpenRead(Path.Combine(relatedPath, "ResultSchema.json"));
        var schemaJsonObj = await SerializationHelper.DeserializeJsonAsync<object>(promptSchema);
        ResultSchemaJson = SerializationHelper.Serialize<object>(schemaJsonObj);

        using FileStream promptJson = File.OpenRead(Path.Combine(relatedPath, "Examples.json"));

        messagesExList = await SerializationHelper.DeserializeJsonAsync<List<FormalizedMessageEx>>(promptJson);
        messagesList = messagesExList.ConvertAll<FormalizedMessage>(m => (FormalizedMessage)m);

        MessagesJson = SerializationHelper.Serialize<IEnumerable<MessageForAnalyzing>>(
            messagesExList.Select(ex => ex.Message));

        FormalizedMessagesJson = SerializationHelper.Serialize<List<FormalizedMessage>>(messagesList);
    }
}