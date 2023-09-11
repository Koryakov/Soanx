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
    public HashSet<SoanxTdUpdateType> ListeningEvents { get; set; }
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

public class TgListeningChat {
    public string Comment { get; set; }
    public long ChatId { get; set; }
}

public class TgMessageGrabbingSettings {
    public int ReadingMessagesInterval { get; set; }
    public int ChatHistoryReadingCount { get; set; }
}

public class TgMessageSavingSettings {
    public int BatchSize { get; set; }
    public int RunsInterval { get; set; }
}

public class TgCurrencyAnalyzingSettings {
    public List<TgCurrencyAnalyzingChat> TgCurrencyAnalyzingChats { get; set; }
}


public class TgCurrencyAnalyzingChat {
    public string Comment { get; set; }
    public long ChatId { get; set; }
    public DateTime ReadTillDate { get; set; }
}

public class OpenAiSettings {
    public required string OpenAiApiKey { get; set; }
}

public class CacheSettings {
    public int DefaultExpirationMinutes { get; set; }
}



