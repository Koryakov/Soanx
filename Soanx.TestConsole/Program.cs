// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Configuration;
using Moq;
using Soanx.Repositories;
using Soanx.TelegramAnalyzer;
using Soanx.TelegramAnalyzer.Models;
using System.Runtime.CompilerServices;
using TdLib;
using static TdLib.TdApi;

internal class Program {
    private static async Task Main(string[] args) {
        await new MyProgram().Main();
    }
}
public class MyProgram {
    public async Task Main() {
        AppSettingsHelper appSettings = new();
        TdClient tdClient = new TdClient();
        ITdClientAuthorizer tdClientAuthorizer = new TdClientAuthorizer(tdClient, appSettings.TdLibParameters, appSettings.BotSettings);
        CancellationToken cancellationToken = new();

        ITelegramWorker tgReader = new TgMessageGrabbingWorker(tdClientAuthorizer);
        await tgReader.Run(cancellationToken);

        Console.WriteLine("Press ENTER to exit from application");
        Console.ReadLine();
    }
}