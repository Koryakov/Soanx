﻿// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Serilog;
using Soanx.Repositories;
using Soanx.TelegramAnalyzer;
using Soanx.TelegramAnalyzer.Models;
using System.Runtime.CompilerServices;
using TdLib;
using static TdLib.TdApi;
using System;
using Microsoft.Extensions.Hosting;
using TdLib.Bindings;
using Soanx.Repositories.Models;
using System.Collections.Concurrent;
using System.Threading;

internal class Program {
    private static async Task Main(string[] args) {

        AppSettingsHelper appSettings = new();
        Log.Logger = new LoggerConfiguration()
           .ReadFrom.Configuration(appSettings.Config)
           .Enrich.FromLogContext()
           .CreateLogger();

        await new SoanxConsole().Main(appSettings);
        Log.CloseAndFlush();
    }
}
public class SoanxConsole {

    public async Task Main(AppSettingsHelper appSettings) {
        CancellationTokenSource cancellationTokenSource = new();
        
        var log = Log.ForContext<SoanxConsole>();
        log.Information("Soanx logging started...");
        var collectionForStoring = new ConcurrentBag<TgMessage> ();
        List<Task> tasks = new List<Task>();

        TdClient tdClient = new TdClient();
        tdClient.Bindings.SetLogVerbosityLevel(TdLogLevel.Fatal);

        ITdClientAuthorizer tdClientAuthorizer = new TdClientAuthorizer(tdClient, appSettings.TdLibParameters, appSettings.BotSettings);
        await tdClientAuthorizer.Run();

        ITelegramWorker tgGrabbingWorker = new TgMessageGrabbingWorker(tdClient, collectionForStoring, appSettings.TgGrabbingSettings, appSettings.TgGrabbingChats);
        ITelegramWorker tgSavingWorker = new TgMessageSavingWorker(appSettings.TgMessageSavingSettings, collectionForStoring, appSettings.SoanxConnectionString);
        ITelegramWorker tgEventsWorker = new TgMessageEventsWorker(tdClient, appSettings.TgListeningChats, appSettings.SoanxConnectionString);

        QueueConfigurations currencyAnalyzerQueueSettings = new() {
            RabbitMqCredentials = appSettings.RabbitMqCredentials,
            QueueMessagingSettings = appSettings.QueueMessagingSettings
        };
        ITelegramWorker tgAnalyzingWorker = new CurrencyAnalyzingWorker(
            appSettings.OpenAiSettings, appSettings.TgCurrencyAnalyzingSettings, 
            appSettings.SoanxConnectionString, appSettings.CacheSettings, currencyAnalyzerQueueSettings);

        //TODO: TgMessageGrabbingWorker and TgMessageSavingWorker must work together. Probably saving worker should run inside grabbing worker.
        //tasks.Add(Task.Run(() => tgGrabbingWorker.Run(cancellationTokenSource.Token)));
        //tasks.Add(Task.Run(() => tgSavingWorker.Run(cancellationTokenSource.Token)));

        tasks.Add(Task.Run(() => tgEventsWorker.Run(cancellationTokenSource.Token)));
        tasks.Add(Task.Run(() => tgAnalyzingWorker.Run(cancellationTokenSource.Token)));

        _ = Task.Run(() => {
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
            cancellationTokenSource.Cancel();
        });
        await Task.WhenAll(tasks);
    }
     
}