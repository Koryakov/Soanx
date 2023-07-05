using Soanx.TgWorker;

namespace Soanx.CurrencyExchange {
    public class Worker : ITgWorker {
       
        public Task Run() {
            return new Task(() => {
                Task.Delay(100);
            });
        }

        public string GetTest() {
            return "test";
        }

    }
}