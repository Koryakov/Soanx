using Serilog;
using Soanx.Repositories;
using Soanx.Repositories.Models;
using Soanx.TelegramAnalyzer.Models;
using Soanx.TgWorker;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soanx.TelegramAnalyzer {
    public class TgMessageSavingWorker : ITelegramWorker {

        private static Serilog.ILogger log = Log.ForContext<TgMessageSavingWorker>();
        private TgRepository tgRepository;
        public ConcurrentBag<TgMessage> CollectionForStoring { get; private set; }
        public int BatchSize { get; private set; }
        public int RunsInterval { get; private set; }

        public TgMessageSavingWorker(TgMessageSavingSettings tgMessageSavingSettings, ConcurrentBag<TgMessage> collectionForStoring,
            string soanxConnectionString) {

            CollectionForStoring = collectionForStoring;
            tgRepository = new TgRepository(soanxConnectionString);
            BatchSize = tgMessageSavingSettings.BatchSize;
            RunsInterval = tgMessageSavingSettings.RunsInterval;
        }

        public async Task Run(CancellationToken cancellationToken) {
            var locLog = log.ForContext("method", "Run()");
            locLog.Information("IN");

            while (!cancellationToken.IsCancellationRequested) {
                var messagesList = new List<TgMessage>(BatchSize);

                for (int i = 0; i < BatchSize; i++) {
                    if (CollectionForStoring.TryTake(out var item)) {
                        messagesList.Add(item);
                    }
                }

                if (messagesList.Count > 0) {
                    locLog.Information("msg in collectionForStoring = {allCollection}, taken to save = {takenCount}", CollectionForStoring.Count, messagesList.Count);

                    try {
                        await tgRepository.SaveTgMessageList(messagesList);
                    
                    } catch (Exception ex) {
                        foreach (var message in messagesList) {
                            CollectionForStoring.Add(message);
                        }
                        locLog.Error(ex, "messages have been returned to the collectionForStoring.");
                    }
                }
                Task.Delay(RunsInterval).Wait();
            }
            locLog.Information("OUT. CancellationToken has been triggered");
        }
    }
}
