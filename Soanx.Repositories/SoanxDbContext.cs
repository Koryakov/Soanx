using Microsoft.EntityFrameworkCore;
using Soanx.CurrencyExchange.EfModels;
using Soanx.Repositories.Models;

namespace Soanx.Repositories
{
    public class SoanxDbContext : DbContext {
        private readonly string connectionString;
        public DbSet<TgMessage> TgMessage { get; set; }
        public DbSet<TgMessage2> TgMessage2 { get; set; }
        public DbSet<TgMessageRaw> TgMessageRaw { get; set; }
        public DbSet<City> City { get; set; }
        public DbSet<Country> Country { get; set; }

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
            modelBuilder.Entity<CityExchangeOffer>()
                .HasKey(ce => new { ce.CityId, ce.ExchangeOfferId });

            modelBuilder.Entity<CityExchangeOffer>()
                .HasOne<City>(ce => ce.City)
                .WithMany(c => c.CityExchangeOffers)
                .HasForeignKey(ce => ce.CityId);

            modelBuilder.Entity<CityExchangeOffer>()
                .HasOne<ExchangeOffer>(ce => ce.ExchangeOffer)
                .WithMany(eo => eo.CityExchangeOffers)
                .HasForeignKey(ce => ce.ExchangeOfferId);

            modelBuilder.Entity<City>()
                .HasOne<Country>(c => c.Country)
                .WithMany()
                .HasForeignKey(c => c.CountryId);

            modelBuilder.Entity<ExchangeOffer>()
                .HasOne<CurrencyOffer>(eo => eo.SellCurrencyOffer)
                .WithMany()
                .HasForeignKey(eo => eo.SellCurrencyOfferId);

            modelBuilder.Entity<ExchangeOffer>()
                .HasOne<CurrencyOffer>(eo => eo.BuyCurrencyOffer)
                .WithMany()
                .HasForeignKey(eo => eo.BuyCurrencyOfferId);

            modelBuilder.Entity<CurrencyOffer>()
                .Property(co => co.ExchangeTypeId)
                .HasConversion<int>();
        }
    }
}
