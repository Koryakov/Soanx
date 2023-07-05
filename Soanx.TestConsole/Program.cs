// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Configuration;
using Moq;
using Soanx.Models;
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

        var tgEngine = new TgEngine();
        await tgEngine.RunAsync();

        Console.WriteLine("Press ENTER to exit from application");
        Console.ReadLine();
    }
}

//var tgRepository = new TelegramRepository(soanxConnectionString);
//var tgMessage = new TgMessage() {
//    SenderId = 1,
//    Text = "ttt",
//    TgMessageId = 666
//};
//tgRepository.AddTgMessage(tgMessage);