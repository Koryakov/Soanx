using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Soanx.CurrencyExchange.Models;

public class EfModels {

    public class ExchangeOffer {
        public int Id { get; set; }
        public long TgMessageId { get; set; }
        public int SellCurrencyOfferId { get; set; }
        public int BuyCurrencyOfferId { get; set; }
        public decimal? RateMin { get; set; }
        public decimal? RateMax { get; set; }
        public DateTime CreatedDateUtc { get; set; }
        public CurrencyOffer SellCurrencyOffer { get; set; }
        public CurrencyOffer BuyCurrencyOffer { get; set; }
        public ICollection<CityExchangeOffer> CityExchangeOffers { get; set; }
    }

    public interface ICurrencyOffer {
        int Id { get; set; }
        decimal? AmountMin { get; set; }
        decimal? AmountMax { get; set; }
        ExchangeType ExchangeTypeId { get; set; }
        List<string> BankNames { get; set; }
    }

    public class CurrencyOffer : ICurrencyOffer {
        public int Id { get; set; }
        public decimal? AmountMin { get; set; }
        public decimal? AmountMax { get; set; }
        public ExchangeType ExchangeTypeId { get; set; }
        public List<string> BankNames { get; set; }
    }

    public class City {
        public int Id { get; set; }
        public string Name { get; set; }
        public int CountryId { get; set; }
        //public Country Country { get; set; }
        public ICollection<CityExchangeOffer> CityExchangeOffers { get; set; }
    }

    public class CityExchangeOffer {
        public int CityId { get; set; }
        public City City { get; set; }
        public int ExchangeOfferId { get; set; }
        public ExchangeOffer ExchangeOffer { get; set; }
    }

    public class Country {
        public int Id { get; set; }
        public string Name { get; set; }
        //public List<City> Cities { get; set; }
    }

    public enum ExchangeType : int {
        Unknown = 0,
        Buy = 1,
        Sell = 2
    }

    public enum OfferType {
        Unknown = 0,
        Buy = 1,
        Sell = 2
    }

    public enum CurrencyEnum {
        Other = 0,
        USDT = 1,
        USDC = 2,
        BUSD = 3,
        EUR = 4,
        USD = 5,
        UAH = 6,
        RUB = 7
    }
}
