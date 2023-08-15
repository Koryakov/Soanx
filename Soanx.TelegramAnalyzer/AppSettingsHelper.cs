
using Microsoft.Extensions.Configuration;
using Soanx.TelegramAnalyzer.Models;
using Soanx.TelegramModels;

namespace Soanx.TelegramAnalyzer;
public class AppSettingsHelper {

    public IConfiguration Config { get; private set; }
    public TdLibParametersModel TdLibParameters { get; private set; }
    public string SoanxConnectionString { get; private set; }
    public TelegramBotSettings BotSettings { get; private set; }
    public TgMessageSavingSettings TgMessageSavingSettings { get; private set; }
    public TgMessageGrabbingSettings TgGrabbingSettings { get; private set; }
    public List<TgGrabbingChat> TgGrabbingChats { get; set; }
    public List<TgListeningChat> TgListeningChats { get; set; }

    public AppSettingsHelper() {
        Config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("serilogsettings.json")
            .AddEnvironmentVariables()
            .Build();

        TdLibParameters = Config.GetRequiredSection("TdLibParameters").Get<TdLibParametersModel>();
        SoanxConnectionString = Config.GetConnectionString("SoanxDbConnection");
        BotSettings = Config.GetRequiredSection("TelegramBotSettings").Get<TelegramBotSettings>();
        TgGrabbingSettings = Config.GetRequiredSection("TgMessageGrabbingSettings").Get<TgMessageGrabbingSettings>();
        TgMessageSavingSettings = Config.GetRequiredSection("TgMessageSavingSettings").Get<TgMessageSavingSettings>();
        TgGrabbingChats = Config.GetRequiredSection("TgGrabbingChats").Get<List<TgGrabbingChat>>();
        TgListeningChats = Config.GetRequiredSection("TgListeningChats").Get<List<TgListeningChat>>();
    }
}