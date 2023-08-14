using Soanx.TelegramModels;
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

public class TelegramBotSettings {
    public string Token { get; set; }
    public long ChatId { get; set; }
}

public class TgGrabbingChat {
    public string Comment { get; set; }
    public long ChatId { get; set; }
    public DateTime ReadTillDate { get; set; }
}

public class TgMessageGrabbingSettings {
    public int ReadingMessagesInterval { get; set; }
    public int SavingMessagesRunsInterval { get; set; }
    public int ChatHistoryReadingCount { get; set; }
    public int SavingMessagesBatchSize { get; set; }
}

//public class TgGrabbingChats {
//    public List<TgChatGrabbingSettings> Chats { get; set; }
//}

//public class WorkerSubscriber {
//    public WorkerPluginSettings Settings { get; set; }
//    public Assembly PluginAssembly { get; set; }
//}



