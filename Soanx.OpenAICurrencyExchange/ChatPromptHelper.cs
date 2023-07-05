using Microsoft.Extensions.Options;
using Soanx.OpenAICurrencyExchange.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace Soanx.OpenAICurrencyExchange;
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

    private JsonSerializerOptions deserializeOptions = new JsonSerializerOptions {
        ReadCommentHandling = JsonCommentHandling.Skip,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private JsonSerializerOptions serializeOptions = new JsonSerializerOptions {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public ChatPromptHelper() {
        
    }

    public async Task InitializePromptCollections(string corpusName) {
        CorpusName = corpusName;

        string relatedPath = Path.Combine("OpenAICorpus", corpusName);
        Instruction = await File.ReadAllTextAsync(Path.Combine(relatedPath, "Instruction.txt"));

        using FileStream promptSchema = File.OpenRead(Path.Combine(relatedPath, "ResultSchema.json"));
        var schemaJsonObj = await JsonSerializer.DeserializeAsync<object>(promptSchema, deserializeOptions);
        ResultSchemaJson = JsonSerializer.Serialize<object>(schemaJsonObj, serializeOptions);

        using FileStream promptJson = File.OpenRead(Path.Combine(relatedPath, "Examples.json"));
        messagesExList = await JsonSerializer.DeserializeAsync<List<FormalizedMessageEx>>(promptJson, deserializeOptions);
        messagesList = messagesExList.ConvertAll<FormalizedMessage>(m => (FormalizedMessage)m);

        MessagesJson = JsonSerializer.Serialize<IEnumerable<Message>>(
            messagesExList.Select(ex => ex.Message), serializeOptions);

        FormalizedMessagesJson = JsonSerializer.Serialize<List<FormalizedMessage>>(
            messagesList, serializeOptions);


    }
}