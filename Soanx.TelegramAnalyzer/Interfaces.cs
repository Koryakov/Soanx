using Soanx.Repositories.Models;
using System.Collections.Concurrent;
using TdLib;

namespace Soanx.TelegramAnalyzer;

public interface ITelegramWorker {
    public Task Run(CancellationToken cancellationToken);
}

public interface ITdClientAuthorizer {
    public TdClient TdClient { get; }
    public Task UpdateReceived(TdApi.Update update);
    public Task Run();
    public void SubscribeToUpdateReceivedEvent();
}