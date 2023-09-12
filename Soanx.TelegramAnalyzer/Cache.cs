using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Caching.Memory;
using Soanx.CurrencyExchange.Models;
using Soanx.Repositories;
using Soanx.TelegramAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soanx.TelegramAnalyzer {
    public class Cache {
        private static class Keys {
            public const string Cities = "Cities";
        }
        
        private MemoryCache cache = new(new MemoryCacheOptions());
        private TgRepository tgRepository;
        public CacheSettings CacheSettings { get; private set; }
        public Cache(CacheSettings cacheSettings, string soanxConnectionString) {
            tgRepository = new TgRepository(soanxConnectionString);
            CacheSettings = cacheSettings;
        }
        public async Task<List<EfModels.City>> GetCities() {

            cache.TryGetValue(Keys.Cities, out List<EfModels.City>? cities);
            if (cities == null) {
                cities = await tgRepository.GetCities();
                var options = new MemoryCacheEntryOptions() {
                    SlidingExpiration = TimeSpan.FromMinutes(CacheSettings.DefaultExpirationMinutes)
                };
                cache.Set(Keys.Cities, cities, options);
            }
            return cities;
        }
    }
}
