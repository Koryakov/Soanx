using Microsoft.EntityFrameworkCore;
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
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public TelegramRepository(string connectionString) : base(connectionString) {
        }

        public async Task AddTgMessageAsync(TgMessage tgMessage) {
            logger.Debug($"IN AddTgMessageAsync()");
            try {
                using (var db = CreateContext()) {
                    await db.TgMessage.AddAsync(tgMessage);
                    bool hasChanges = db.ChangeTracker.HasChanges();
                    int count = await db.SaveChangesAsync();
                    logger.Debug($"OUT AddTgMessageAsync() hasChanges = {hasChanges}; count = {count}; tgMssageId = {tgMessage.TgMessageId}");
                }
            } catch (Exception ex) {
                logger.Error(ex);
            }
        }

        public async Task<int> AddTgMessageAsync(List<TgMessage> tgMessageList) {
            try {
                using (var db = CreateContext()) {
                    await db.TgMessage.AddRangeAsync(tgMessageList);
                    //int count = await db.SaveChangesAsync();
                    int count = await db.SaveChangesAsync();
                    logger.Debug($"OUT AddTgMessageAsync() count = {count};");
                    return count;
                }
            }
            catch (Exception ex) {
                logger.Error(ex);
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

        public async Task SaveTgMessageRawListAsync(List<TgMessageRaw> tgMessageRawList) {
            var existingChatsIds = tgMessageRawList.Select(r => r.TgChatId).ToList();
            var existingMessagesIds = tgMessageRawList.Select(r => r.TgChatMessageId).ToList();
            using (var db = CreateContext()) {
                using (var transaction = await db.Database.BeginTransactionAsync()) {
                    try {
                        var existingIds = await db.TgMessageRaw.Where(
                            m => 
                            existingMessagesIds.Contains(m.TgChatMessageId)
                            &&
                            existingChatsIds.Contains(m.TgChatId)
                            ).Select(r => new TgMessageRaw() { 
                                TgChatId = r.TgChatId,
                                TgChatMessageId = r.TgChatMessageId
                            }).AsNoTracking().ToListAsync();

                        var comparer = new TgMessageRawComparer();
                        var filteredMessages = tgMessageRawList.Where(message => !existingIds.Contains(message, comparer)).ToList();

                        await db.TgMessageRaw.AddRangeAsync(filteredMessages);
                        await db.SaveChangesAsync();
                        transaction.Commit();
                    }
                    catch (Exception ex) {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
    }
}
