
using Microsoft.Extensions.Configuration;
using Soanx.TelegramAnalyzer.Models;
using Soanx.TelegramModels;

namespace Soanx.TelegramAnalyzer;
public class AppSettingsHelper {

    public IConfiguration Config { get; private set; }
    public TdLibParametersModel TdLibParameters { get; private set; }
    public string SoanxConnectionString { get; private set; }
    public TelegramBotSettings BotSettings { get; private set; }
    public List<TgGrabbingChatsSettings> TgGrabbingChatsSettings { get; set; }

    public AppSettingsHelper() {
        Config = new ConfigurationBuilder().AddJsonFile("appsettings.json").AddEnvironmentVariables().Build();
        TdLibParameters = Config.GetRequiredSection("TdLibParameters").Get<TdLibParametersModel>();
        SoanxConnectionString = Config.GetConnectionString("SoanxDbConnection");
        BotSettings = Config.GetRequiredSection("TelegramBotSettings").Get<TelegramBotSettings>();
        TgGrabbingChatsSettings = Config.GetRequiredSection("TgGrabbingChatsSettings").Get<List<TgGrabbingChatsSettings>>();
    }
}