// See https://aka.ms/new-console-template for more information

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
           //.Enrich.FromLogContext()
           .CreateLogger();

        await new SoanxConsole().Main(appSettings);
        Log.CloseAndFlush();
    }
}
public class SoanxConsole {

    public async Task Main(AppSettingsHelper appSettings) {
        var log = Log.ForContext<SoanxConsole>();
        log.Information("Soanx logging started...");
        var collectionForStoring = new ConcurrentBag<TgMessageRaw> ();

        TdClient tdClient = new TdClient();
        tdClient.Bindings.SetLogVerbosityLevel(TdLogLevel.Fatal);

        ITdClientAuthorizer tdClientAuthorizer = new TdClientAuthorizer(tdClient, appSettings.TdLibParameters, appSettings.BotSettings);
        CancellationTokenSource cancellationTokenSource = new();

        List<Task> tasks = new List<Task>();

        ITelegramWorker tgReader = new TgMessageGrabbingWorker(tdClientAuthorizer, collectionForStoring, appSettings.TgGrabbingSettings, appSettings.TgGrabbingChats);
        ITelegramWorker savingWorker = new TgMessageSavingWorker(appSettings.TgMessageSavingSettings, collectionForStoring);
        
        tasks.Add(Task.Run(() => tgReader.Run(cancellationTokenSource.Token)));
        tasks.Add(Task.Run(() => savingWorker.Run(cancellationTokenSource.Token)));

        var userInputTask = Task.Run(() =>
        {
            Console.WriteLine("Press ENTER to exit from application");
            Console.ReadLine();
            cancellationTokenSource.Cancel();
        });
        await Task.WhenAll(tasks);
    }

}