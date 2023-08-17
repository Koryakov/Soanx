using Microsoft.EntityFrameworkCore;
using Soanx.Repositories.Models;

namespace Soanx.Repositories
{
    public class SoanxDbContext : DbContext {
        private readonly string connectionString;
        public DbSet<TgMessage2> TgMessage { get; set; }
        public DbSet<TgMessageRaw> TgMessageRaw { get; set; }

        public SoanxDbContext(DbContextOptions<SoanxDbContext> options) : base(options) {
        }

        public SoanxDbContext(string connectionString) : base() {
            this.connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder builder) {
            if (!builder.IsConfigured) {
                builder.UseNpgsql(connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<TgMessageRaw>()
                .HasIndex(p => new { p.TgChatId, p.TgChatMessageId, p.UpdateType })
                .IsUnique();
        }
    }
}
