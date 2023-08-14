using Microsoft.EntityFrameworkCore;
using Serilog;
using Soanx.Repositories.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Soanx.Repositories
{
    public class TelegramRepository : SoanxDbRepositoryBase {
        private Serilog.ILogger log;

        public TelegramRepository(string connectionString) : base(connectionString) {
            log = Log.ForContext<TelegramRepository>();
        }

        public async Task AddTgMessageAsync(TgMessage tgMessage) {
            log.Debug($"IN AddTgMessageAsync()");
            try {
                using (var db = CreateContext()) {
                    await db.TgMessage.AddAsync(tgMessage);
                    bool hasChanges = db.ChangeTracker.HasChanges();
                    int count = await db.SaveChangesAsync();
                    log.Debug($"OUT AddTgMessageAsync() hasChanges = {hasChanges}; count = {count}; tgMssageId = {tgMessage.TgMessageId}");
                }
            } catch (Exception ex) {
                log.Error(ex, "AddTgMessageAsync() exception");
            }
        }

        public async Task<int> AddTgMessageAsync(List<TgMessage> tgMessageList) {
            try {
                using (var db = CreateContext()) {
                    await db.TgMessage.AddRangeAsync(tgMessageList);
                    //int count = await db.SaveChangesAsync();
                    int count = await db.SaveChangesAsync();
                    log.Debug($"OUT AddTgMessageAsync() count = {count};");
                    return count;
                }
            }
            catch (Exception ex) {
                log.Error(ex, "AddTgMessageAsync() exception");
                return 0;
            }
        }

        //public async Task<List<TgMessage>> GetNotExtractedTgMessagesBatch(long chatId, DateTime sinceDate, int count) {
        //    using (var db = CreateContext()) {
        //        //TODO: order must be changed to telegram message Date field - see RawData column. Date should be moved to separate table column.
        //        return await db.TgMessage.Where(m => m.ExtractedFacts == null && m.CreatedDate <= sinceDate)
        //            .OrderByDescending(m => m.CreatedDate).Take(count).ToListAsync();
        //    }
        //}

        public async Task<List<TgMessage>> GetLastNotExtractedTgMessages(long chatId, DateTime sinceDate, int limit) {
            using (var db = CreateContext()) {
                //TODO: order must be changed to telegram message Date field - see RawData column. Date should be moved to separate table column.
                return await db.TgMessage.Where(m => m.ExtractedFacts == null && m.CreatedDate <= sinceDate)
                    .OrderByDescending(m => m.CreatedDate).Take(limit).ToListAsync();
            }
        }

        public async Task SaveTgMessageRawList(List<TgMessageRaw> tgMessageRawList) {
            var locLog = log.ForContext("method", "SaveTgMessageRawList()");
            locLog.Information("IN, list count = {Count}", tgMessageRawList.Count);

            var existingChatsIds = tgMessageRawList.Select(r => r.TgChatId).Distinct().ToList();
            var existingMessagesIds = tgMessageRawList.Select(r => r.TgChatMessageId).ToList();
            locLog.Verbose("New portion data: chatIds: {@chatIds}, msgIds: {@msgIds}", existingChatsIds, existingMessagesIds);

            using (var db = CreateContext()) {
                using (var transaction = await db.Database.BeginTransactionAsync()) {
                    try {
                        var query = db.TgMessageRaw.Where(
                           dbMessages =>
                               existingChatsIds.Contains(dbMessages.TgChatId)
                               &&
                               existingMessagesIds.Contains(dbMessages.TgChatMessageId)
                           ).Select(r => new TgMessageRaw() {
                               TgChatId = r.TgChatId,
                               TgChatMessageId = r.TgChatMessageId
                           });

                        var dbExistingMessages = await query.AsNoTracking().ToListAsync();
                        locLog.Information("existing messages count = {existingCount}; Ids from db:  {@dbMessages}", dbExistingMessages.Count, dbExistingMessages.Select(e => e.TgChatMessageId));

                        var comparer = new TgMessageRawComparer();
                        var filteredMessages = tgMessageRawList.Where(message => !dbExistingMessages.Contains(message, comparer)).ToList();
                        locLog.Information("filtered msg count = {filteredMsgCount}", filteredMessages.Count);
                        
                        if (filteredMessages.Count > 0) {
                            await db.TgMessageRaw.AddRangeAsync(filteredMessages);
                            int savedCount = await db.SaveChangesAsync();
                            await transaction.CommitAsync();
                            locLog.Information("savedCount = {savedCount}.  Transaction committed", savedCount);
                        } else {
                            locLog.Information("No messages to save");
                        }
                    }
                    catch (Exception ex) {
                        transaction.Rollback();
                        locLog.Error(ex, "transaction has been rollbacked");
                        throw;
                    }
                }
            }
        }
    }
}
