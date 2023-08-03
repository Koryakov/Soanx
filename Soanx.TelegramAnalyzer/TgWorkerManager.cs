using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Soanx.Repositories.Models;
using Soanx.TelegramAnalyzer.Models;
using Soanx.TgWorker;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using static TdLib.TdApi;
using TdLib;
using Soanx.TelegramModels;

namespace Soanx.TelegramAnalyzer {    

    public class PluginData {
        public Assembly Assembly { get; set; }
        public string TypeName { get; set; }
        public List<long> ChatIds { get; set; }
    }
    public class TgWorkerManager {

        private IConfiguration config;
        public List<WorkerPluginSettings> PluginSettings { get; private set; }
        //chatId, eventType, pluginGuid

        public Dictionary<Guid, PluginData> PluginsDictionary { get; private set; }
        public Dictionary<long, Dictionary<UpdateType, List<Guid>>> ChatEventPluginDictionary { get; private set; }
        public Dictionary<UpdateType, List<Guid>> EventsTable { get; private set; }

        public TgWorkerManager() {
           
        }

        private async Task LoadWorkersSettings() {
            PluginSettings = new List<WorkerPluginSettings>();
            config = new ConfigurationBuilder().AddJsonFile("appsettings.json").AddEnvironmentVariables().Build();
            config.GetSection("WorkerPlugins").Bind(PluginSettings);
        }
        private async Task InitEventsTable() {
            EventsTable = new Dictionary<UpdateType, List<Guid>>();
            ChatEventPluginDictionary = new Dictionary<long, Dictionary<UpdateType, List<Guid>>>();
            
            foreach (var enumItem in Enum.GetValues<UpdateType>()) {
                EventsTable.Add(enumItem, new List<Guid>());
            }

            //TODO: SHould be refactored: group chats and events by worker plugins
            foreach (var pluginSettings in PluginSettings) {
                foreach (var listeningEvent in pluginSettings.ListeningEvents) {
                    EventsTable[listeningEvent].Add(pluginSettings.UniqueGuid);
                }
                foreach (var chatId in pluginSettings.ListeningChatIds) {
                    ChatEventPluginDictionary.TryAdd(chatId, EventsTable);
                }
            }
        }
        public async Task LoadWorkersAssemblies() {
            LoadWorkersSettings();
            InitEventsTable();

            PluginsDictionary = new();

            foreach (var pluginSettings in PluginSettings) {
                Assembly pluginAssembly = Assembly.LoadFile(pluginSettings.AssemblyPath);
                PluginsDictionary.Add(pluginSettings.UniqueGuid, new() {
                    TypeName  = pluginSettings.FullyQualifiedWorkerTypeName,
                    Assembly = pluginAssembly
                });
            }

        }

        public IEnumerable<ITgWorker> CreateWorkersForEvent(long chatId, UpdateType eventType) {
            List<ITgWorker> list = new();
            if(ChatEventPluginDictionary.ContainsKey(chatId)) {
                List<Guid> pluginGuids;
                if (ChatEventPluginDictionary[chatId].TryGetValue(eventType, out pluginGuids)) {
                    foreach (var pluginGuid in pluginGuids) {
                        PluginData pluginData = PluginsDictionary[pluginGuid];
                        var pluginInstance = pluginData.Assembly.CreateInstance(pluginData.TypeName) as ITgWorker;
                        list.Add(pluginInstance);
                        //yield return (ITgWorker)AssemblyCacheDictionary[pluginGuid].CreateInstance(PluginTypeNameDictionary[pluginGuid]);
                    }
                }
            }
            return list;
        }
    }
}
