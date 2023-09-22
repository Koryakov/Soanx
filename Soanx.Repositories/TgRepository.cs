using Microsoft.EntityFrameworkCore;
using Serilog;
using Soanx.CurrencyExchange;
using Soanx.CurrencyExchange.Models;
using Soanx.Repositories.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Soanx.Repositories
{
    public class TgRepository : SoanxDbRepositoryBase {
        private Serilog.ILogger log = Log.ForContext<TgRepository>();

        public TgRepository(string connectionString) : base(connectionString) {
            log = Log.ForContext<TgRepository>();
        }

        public async Task AddTgMessage(List<TgMessage> tgMessageList) {
            var locLog = log.ForContext("method", "AddTgMessage(List<TgMessage>)");
            locLog.Information("IN, msgIds:{@msgIds}", tgMessageList.Select(l => l.TgMessageId));
            using (var db = CreateContext()) {
                await db.TgMessage.AddRangeAsync(tgMessageList);
                int count = await db.SaveChangesAsync();
                locLog.Debug($"saved count = {count};");
            }
        }

        public async Task AddTgMessage(TgMessage newTgMessage) {
            var locLog = log.ForContext("method", "AddTgMessage(TgMessage)");
            locLog.Debug("IN, saving chatId = {chatId}, msgId = {msgId}", newTgMessage.TgChatId, newTgMessage.TgMessageId);

            using (var db = CreateContext()) {
                await db.TgMessage.AddAsync(newTgMessage);
                await db.SaveChangesAsync();
            }
        }

        public async Task SaveTgMessageList(List<TgMessage> tgMessageList) {
            var locLog = log.ForContext("method", "SaveTgMessageList()");
            locLog.Information("IN, list count = {Count}", tgMessageList.Count);

            var existingChatsIds = tgMessageList.Select(r => r.TgChatId).Distinct().ToList();
            var existingMessagesIds = tgMessageList.Select(r => r.TgMessageId).ToList();
            locLog.Debug("New portion data: chatIds = {@chatIds}, msgIds: {@msgIds}", existingChatsIds, existingMessagesIds);

            using (var db = CreateContext()) {
                using (var transaction = await db.Database.BeginTransactionAsync()) {
                    try {
                        var query = db.TgMessage.Where(
                           dbMessages =>
                               existingChatsIds.Contains(dbMessages.TgChatId)
                               &&
                               existingMessagesIds.Contains(dbMessages.TgMessageId)
                           ).Select(r => new TgMessage() {
                               TgChatId = r.TgChatId,
                               TgMessageId = r.TgMessageId
                           });

                        var dbExistingMessages = await query.AsNoTracking().ToListAsync();
                        locLog.Information("Existing messages count = {existingCount}; Ids from db:  {@dbMessages}", 
                            dbExistingMessages.Count, dbExistingMessages.Select(e => e.TgMessageId));

                        var comparer = new TgMessageComparer();
                        var filteredMessages = tgMessageList.Where(message => !dbExistingMessages.Contains(message, comparer)).ToList();
                        locLog.Information("Filtered msg count = {filteredMsgCount}", filteredMessages.Count);

                        if (filteredMessages.Count > 0) {
                            await db.TgMessage.AddRangeAsync(filteredMessages);
                            int savedCount = await db.SaveChangesAsync();
                            await transaction.CommitAsync();
                            locLog.Debug("SavedCount = {savedCount}.  Transaction committed", savedCount);
                        }
                        else {
                            locLog.Debug("No messages to save");
                        }
                    }
                    catch (Exception ex) {
                        transaction.Rollback();
                        locLog.Error(ex, "Transaction has been rollbacked");
                        throw;
                    }
                }
            }
        }

        public async Task<(bool isSuccess, List<DtoModels.MessageToAnalyzing>? messages)> GetTgMessagesByAnalyzedStatus(int minReturningCount,
            TgMessage.TgMessageAnalyzedStatus currentStatus, TgMessage.TgMessageAnalyzedStatus newStatus) {

            var locLog = log.ForContext("method", "GetTgMessagesByAnalyzedStatus");
            locLog.Verbose("IN, count:{@count}, analyzedStatus={@analyzedStatus}", minReturningCount, currentStatus);

            using (var db = CreateContext()) {
                using (var transaction = db.Database.BeginTransaction(IsolationLevel.ReadCommitted)) {
                    try {
                        int count = await db.TgMessage.CountAsync(m => m.AnalyzedStatus == currentStatus);
                        if (count >= minReturningCount) {

                            var modifiedDateUTC = DateTime.UtcNow;

                            var tgMessages = await db.TgMessage
                                .Where(m => m.AnalyzedStatus == currentStatus)
                                .Take(minReturningCount).ToListAsync();

                            tgMessages.ForEach(ca => {
                                ca.AnalyzedStatus = newStatus;
                                ca.AnalyzedStatusModifiedDateUTC = modifiedDateUTC;
                            });
                            db.SaveChanges();
                            transaction.Commit();

                            if (tgMessages.Count > 0) {
                                locLog.Debug("{@cnt} messages retrieved", tgMessages.Count);
                            }
                            return (true, tgMessages.Select(m => new DtoModels.MessageToAnalyzing() { Id = m.Id, Text = m.Text }).ToList());
                        }
                        return (false, null);
                    }
                    catch (Exception ex) {
                        transaction.Rollback();
                        locLog.Error(ex, "Transaction has been rollbacked");
                        throw;
                    }
                }
            }
        }

        public async Task<List<EfModels.City>> GetCities() {
            var locLog = log.ForContext("method", "GetCities");
            locLog.Debug("IN");
            using (var db = CreateContext()) {
                var cities = await db.City.AsNoTracking().ToListAsync();

                locLog.Debug("{@cnt} cities retrieved", cities.Count);
                return cities;
            }
        }

        public async Task SaveExchangeOffers(List<EfModels.ExchangeOffer> exchangeOffers) {
            var locLog = log.ForContext("method", "SaveExchangeOffers()");
            locLog.Debug<List<EfModels.ExchangeOffer>>("IN, exchangeOffers: {@exchangeOffers}", exchangeOffers);

            var utcNow = DateTime.UtcNow;
            exchangeOffers.ForEach(eo => eo.CreatedDateUtc = utcNow);

            using (var db = CreateContext()) {
                await db.ExchangeOffer.AddRangeAsync(exchangeOffers);
                await db.SaveChangesAsync();
            }
        }
    }
}
