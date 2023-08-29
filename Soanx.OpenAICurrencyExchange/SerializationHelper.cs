using System.Text.Encodings.Web;
using System.Text.Json;

namespace Soanx.CurrencyExchange;
public class SerializationHelper {

    public static JsonSerializerOptions SerializeOptions { get; private set; }
    public static JsonSerializerOptions DeserializeOptions { get; private set; }

    static SerializationHelper() {

        SerializeOptions = new JsonSerializerOptions {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        DeserializeOptions = new JsonSerializerOptions {
            ReadCommentHandling = JsonCommentHandling.Skip,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
    }

    public static string Serialize<T>(T obj) {
        return JsonSerializer.Serialize(obj, SerializeOptions);
    }

    public static async Task<T> DeserializeJsonAsync<T>(Stream stream) {
        return await JsonSerializer.DeserializeAsync<T>(stream, DeserializeOptions);
    }

    public static T DeserializeJson<T>(string json) {
        return JsonSerializer.Deserialize<T>(json, DeserializeOptions);
    }
}