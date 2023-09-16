using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog;
using Soanx.CurrencyExchange;
using Soanx.CurrencyExchange.Models;
using Soanx.Repositories.Models;
using System.Text.Json;
using static Soanx.CurrencyExchange.Models.EfModels;

namespace Soanx.Repositories
{
    public class SoanxDbContext : DbContext {

        private ILoggerFactory loggerFactory;
        private readonly string connectionString;
        public DbSet<TgMessage> TgMessage { get; set; }
        public DbSet<TgMessage2> TgMessage2 { get; set; }
        public DbSet<TgMessageRaw> TgMessageRaw { get; set; }
        public DbSet<EfModels.ExchangeOffer> ExchangeOffer { get; set; }
        public DbSet<EfModels.CurrencyOffer> CurrencyOffer { get; set; }
        //public DbSet<EfModels.CityExchangeOffer> CityExchangeOffer { get; set; }
        public DbSet<EfModels.City> City { get; set; }
        public DbSet<EfModels.Country> Country { get; set; }

        public SoanxDbContext(DbContextOptions<SoanxDbContext> options) : base(options) {
            InitializeLoggerFactory();
        }

        public SoanxDbContext(string connectionString) : base() {
            InitializeLoggerFactory();
            this.connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder builder) {
            if (!builder.IsConfigured) {
                builder.UseNpgsql(connectionString)
                    .UseLoggerFactory(loggerFactory)
                    .EnableSensitiveDataLogging();
            }
        }

        private void InitializeLoggerFactory() {
            loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog(Log.Logger);
            });
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {

            //Many-to-many City - ExchangeOffer
            modelBuilder.Entity<CityExchangeOffer>()
           .HasKey(ce => new { ce.CityId, ce.ExchangeOfferId });

            modelBuilder.Entity<CityExchangeOffer>()
                .HasOne(ce => ce.City)
                .WithMany(c => c.CityExchangeOffers)
                .HasForeignKey(ce => ce.CityId);

            modelBuilder.Entity<CityExchangeOffer>()
                .HasOne(ce => ce.ExchangeOffer)
                .WithMany(e => e.CityExchangeOffers)
                .HasForeignKey(ce => ce.ExchangeOfferId);
        
            //modelBuilder.Entity<EfModels.ExchangeOffer>()
        //    .HasMany<EfModels.City>(e => e.Cities)
        //    .WithMany(c => c.ExchangeOffers);

        //modelBuilder.Entity<EfModels.City>()
        //    .HasOne<EfModels.Country>(c => c.Country)
        //    .WithMany()
        //    .HasForeignKey(c => c.CountryId);

        modelBuilder.Entity<EfModels.ExchangeOffer>()
                .HasOne<EfModels.CurrencyOffer>(eo => eo.SellCurrencyOffer)
                .WithMany()
                .HasForeignKey(eo => eo.SellCurrencyOfferId);

            modelBuilder.Entity<EfModels.ExchangeOffer>()
                .HasOne<EfModels.CurrencyOffer>(eo => eo.BuyCurrencyOffer)
                .WithMany()
                .HasForeignKey(eo => eo.BuyCurrencyOfferId);

            modelBuilder.Entity<EfModels.CurrencyOffer>()
                .Property(co => co.ExchangeTypeId)
                .HasConversion<int>();

            modelBuilder.Entity<CurrencyOffer>()
               .Property(e => e.BankNames)
               .HasColumnType("jsonb");
        }
    }
}
