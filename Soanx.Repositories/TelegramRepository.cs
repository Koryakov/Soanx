using Microsoft.EntityFrameworkCore;
using Soanx.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soanx.Repositories {
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
    }
}
