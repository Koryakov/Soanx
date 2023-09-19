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
        public ICollection<CurrencyOfferCurrency> CurrencyOfferCurrencies { get; set; }
    }

    public class CurrencyOfferCurrency {
        public int CurrencyOfferId { get; set; }
        public CurrencyOffer CurrencyOffer { get; set; }
        public int CurrencyId { get; set; }
        public Currency Currency { get; set; }
    }

    public class Currency {
        public int Id { get; set; }
        public string Name { get; set; }

        public static CurrencyEnum GetCurrencyEnumByName(string currencyName) {
            switch(currencyName) {
                case "USDT": return CurrencyEnum.USDT;
                case "USDC": return CurrencyEnum.USDC;
                case "BUSD": return CurrencyEnum.BUSD;
                case "EUR": return CurrencyEnum.EUR;
                case "USD": return CurrencyEnum.USD;
                case "UAH": return CurrencyEnum.UAH;
                case "RUB": return CurrencyEnum.RUB;
                default: return CurrencyEnum.Other;
            }
        }
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

    public class NotMatchedTgMessage {
        public int Id { get; set; }
        public long TgMessageId { get; set; }
        public DateTime CreatedDateUtc { get; set; }
        public NotMatchedTgMessageStatusEnum Status { get; set; }
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

    public enum NotMatchedTgMessageStatusEnum {
        NotProcessed = 0
    }
}
