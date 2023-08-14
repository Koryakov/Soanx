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

        TdClient tdClient = new TdClient();
        tdClient.Bindings.SetLogVerbosityLevel(TdLogLevel.Fatal);

        ITdClientAuthorizer tdClientAuthorizer = new TdClientAuthorizer(tdClient, appSettings.TdLibParameters, appSettings.BotSettings);
        CancellationToken cancellationToken = new();

        ITelegramWorker tgReader = new TgMessageGrabbingWorker(tdClientAuthorizer);
        await tgReader.Run(cancellationToken);

        Console.WriteLine("Press ENTER to exit from application");
        Console.ReadLine();
    }

}