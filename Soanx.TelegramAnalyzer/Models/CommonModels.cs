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

public class RabbitMqCredentials     {
    public string Hostname { get; set; }
    public int Port { get; set; }
    public string VirtualHost { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
}

public class QueueMessagingSettings {
    public QueueSettings MessagesToAnalyzeSettings { get; set; }
    public QueueSettings FormalizedMessagesSettings { get; set; }
    public QueueSettings NotMatchedMessagesSettings { get; set; }
}
public class QueueSettings {
    public string ExchangeName { get; set; }
    public string QueueName { get; set; }
    public string RoutingKey { get; set; }
    public bool Durable { get; set; }
    public bool Exclusive { get; set; }
    public bool AutoDelete { get; set; }
    public int PrefetchCount { get; set; }
}

public class QueueConfigurations {
    public RabbitMqCredentials RabbitMqCredentials { get; set; }
    public QueueMessagingSettings QueueMessagingSettings { get; set; }
}



