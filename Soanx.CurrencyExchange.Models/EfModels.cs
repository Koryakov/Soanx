using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soanx.CurrencyExchange.Models;

public class EfModels {

    public class FormalizedMessage {
        public long Id { get; set; }
        public bool? NotMatched { get; set; }
        public List<ExchangeOffer> ExchangeOffers { get; set; }
    }

    public class ExchangeOffer {
        public int Id { get; set; }
        public int TgMessageId { get; set; }
        public int SellCurrencyOfferId { get; set; }
        public int BuyCurrencyOfferId { get; set; }
        public decimal? RateMin { get; set; }
        public decimal? RateMax { get; set; }
        public CurrencyOffer SellCurrencyOffer { get; set; }
        public CurrencyOffer BuyCurrencyOffer { get; set; }
        public ICollection<CityExchangeOffer> CityExchangeOffers { get; set; }
    }

    public class CurrencyOffer {
        public int Id { get; set; }
        public int ExchangeTypeId { get; set; }
        public decimal? AmountMin { get; set; }
        public decimal? AmountMax { get; set; }
        public ExchangeType ExchangeType { get; set; }
    }

    public class Bank {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class City {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? CountryId { get; set; }
        public Country Country { get; set; }
        public ICollection<CityExchangeOffer> CityExchangeOffers { get; set; }
    }

    public class CityExchangeOffer {
        public int CityId { get; set; }
        public int ExchangeOfferId { get; set; }
        public City City { get; set; }
        public ExchangeOffer ExchangeOffer { get; set; }
    }

    public class Country {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<City> Cities { get; set; }
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
        Unknown = 0,
        USDT = 1,
        USDC = 2,
        BUSD = 3,
        EUR = 4,
        USD = 5,
        UAH = 6,
        RUB = 7
    }
}
