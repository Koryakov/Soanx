
using Soanx.Models;
using System.Reflection;

namespace Soanx.TelegramAnalyzer.Models;
public class WorkerPluginSettings {
    public int Id { get; set; }
    public Guid UniqueGuid { get; set; }
    public string AssemblyPath { get; set; }
    public string FullyQualifiedWorkerTypeName { get; set; }
    public string FriendlyName { get; set; }
    public HashSet<long> ListeningChatIds { get; set; }
    public HashSet<UpdateType> ListeningEvents { get; set; }
}

//public class WorkerSubscriber {
//    public WorkerPluginSettings Settings { get; set; }
//    public Assembly PluginAssembly { get; set; }
//}

public class TdLibParametersModel {
    public required int ApiId { get; set;}
    public required string ApiHash { get; set;}
    public  string PhoneNumber { get; set;}
    public required string ApplicationVersion { get; set;}
    public required string DeviceModel { get; set; }
    public required string SystemLanguageCode { get; set; }
    public required string DatabaseDirectory { get; set; }
    public required string FilesDirectory { get; set; }
}

