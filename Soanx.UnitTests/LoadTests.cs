using Microsoft.Extensions.Configuration;
using Moq;
using Soanx.TelegramAnalyzer;
using Soanx.TelegramAnalyzer.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TdLib;
using static TdLib.TdApi;
using static TdLib.TdApi.MessageContent;
using static TdLib.TdApi.Update;

namespace Soanx.UnitTests {

    public class MockTgEngine : TgEngine {
        private event EventHandler<TdApi.Update> mockUpdateReceivedEvent;
        public MockTgEngine() { }
        public override void SubscribeToUpdateReceivedEvent() {
            mockUpdateReceivedEvent += async (_, update) => { await UpdateReceived(update); };
        }
        public override async Task HandleAuthentication() {
        }
        public void RaiseMockUpdateReceivedEvent(object? sender, Update update) {
            mockUpdateReceivedEvent?.Invoke(this, update);
        }
    }
    public class LoadTests {

        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public event EventHandler<TdApi.Update> updateEvent;
        private long testChatId = -802566439;

        public LoadTests() {
            
        }

        [Fact]
        public async Task LoadTest() {
            logger.Info("IN load test");
            var mockTgEngine = new MockTgEngine();
            var task = Task.Factory.StartNew(async () => { await mockTgEngine.RunAsync(); });
            mockTgEngine.RaiseMockUpdateReceivedEvent(this, new Mock<TdApi.Update.UpdateUser>().Object);
            await task.Unwrap();

            for (int i = 0; i < 3; i++) {
                //await Task.Delay(500);
                var msg = new UpdateNewMessage() {
                    Message = new Message() {
                        ChatId = testChatId,
                        Content = new MessageText() {
                            Text = new FormattedText() {
                                Text = $"msg {i} {DateTime.Now}"
                            }
                        }
                    }
                };
                mockTgEngine.RaiseMockUpdateReceivedEvent(this, msg);
                logger.Info("test event invoked");
            }
            logger.Info("OUT load test");
        }
    }
}
