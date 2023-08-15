using Soanx.TelegramAnalyzer;
using Soanx.TelegramAnalyzer.Models;
using Soanx.TelegramModels;
using Soanx.TgWorker;

namespace Soanx.UnitTests {

    public class PluginLoaderFixture : IDisposable {
        public TgWorkerManager manager = new TgWorkerManager();
        public bool Initiated { get; protected set; }

        public PluginLoaderFixture() {
            var init = LoadAsyncData();
        }

        public async Task LoadAsyncData() {
            await manager.LoadWorkersAssemblies();
            Initiated = true;
        }

        public void Dispose() {
        }
    }
    public class PluginLoaderTests : IClassFixture<PluginLoaderFixture> {
        readonly PluginLoaderFixture fixt;

        public PluginLoaderTests(PluginLoaderFixture fixture) {
            this.fixt = fixture;
        }

        [Fact]
        public async Task LoadPluginSettingsTest() {
            Assert.True(fixt.manager.PluginSettings.Count == 2);
        }
        
        [Fact]
        public async Task CallPluginMethodTest() {
            IEnumerable<ITgWorker> instancesForNew = fixt.manager.CreateWorkersForEvent(-802566439, SoanxTdUpdateType.UpdateNewMessage);
            Assert.Equal(1, instancesForNew.Count());
            var exception = Record.ExceptionAsync(() => instancesForNew.First().Run());
            //Assert.IsNull(exception);

            IEnumerable<ITgWorker> instancesForEdited = fixt.manager.CreateWorkersForEvent(-802566439, SoanxTdUpdateType.UpdateMessageContent);
            Assert.Equal(2, instancesForEdited.Count());

            IEnumerable<ITgWorker> instancesForNone = fixt.manager.CreateWorkersForEvent(-802566439, SoanxTdUpdateType.None);
            Assert.Equal(0, instancesForNone.Count());
        }


    }
}