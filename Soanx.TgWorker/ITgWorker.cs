namespace Soanx.TgWorker {
    public interface ITgWorker {
        public Task Run();
        public string GetTest();
    }
}