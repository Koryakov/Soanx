﻿using Microsoft.EntityFrameworkCore;
using Soanx.Models;

namespace Soanx.Repositories {
    public class SoanxDbContext : DbContext {
        private readonly string connectionString;
        public DbSet<TgMessage> TgMessage { get; set; }

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
    }
}
