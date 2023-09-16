using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;
using static Soanx.CurrencyExchange.Models.DtoModels;
using static Soanx.CurrencyExchange.Models.EfModels;

namespace Soanx.CurrencyExchange.Models;
public class ModelsConvertor {
    public static List<EfModels.ExchangeOffer> ConvertDtoToEf(
        List<DtoModels.FormalizedMessage> dtoMessages, Dictionary<string, EfModels.City> cityDictionary) {

        var exchangeOfferList = new List<EfModels.ExchangeOffer>();

        foreach (FormalizedMessage dtoMessage in dtoMessages) {
            foreach (Offer dtoOffer in dtoMessage.Offers) {
                var exchangeOffer = new EfModels.ExchangeOffer();
                exchangeOffer.TgMessageId = dtoMessage.Id;

                exchangeOffer.SellCurrencyOffer = (CurrencyOffer)InitCurrencyOffer(ExchangeType.Sell, dtoOffer.Sell);
                exchangeOffer.BuyCurrencyOffer = (CurrencyOffer)InitCurrencyOffer(ExchangeType.Buy, dtoOffer.Buy);

                exchangeOffer.RateMax = dtoOffer.RateMax;
                exchangeOffer.RateMin = dtoOffer.RateMin;

                var cityNames = Enumerable.Empty<string>();
                if (dtoOffer.Cities?.Count() > 0) {
                    cityNames = dtoOffer.Cities;
                }
                else if (dtoMessage.Cities?.Count() > 0) {
                    cityNames = dtoMessage.Cities;
                }
                exchangeOffer.CityExchangeOffers = GetOfferCities(exchangeOffer, cityNames);
                exchangeOfferList.Add(exchangeOffer);
            }
        }
        return exchangeOfferList;

        ICurrencyOffer InitCurrencyOffer(ExchangeType exchangeType, CurrencyInfo currencyInfo) {
            if (currencyInfo == null
                ||
                currencyInfo.AmountMax == null
                && currencyInfo.AmountMin == null
                && currencyInfo.Banks != null && currencyInfo.Banks.Count() == 0) {
                return null;
            }
            else {
                var currencyOffer = new CurrencyOffer() {
                    ExchangeTypeId = ExchangeType.Buy,
                    AmountMax = currencyInfo.AmountMax,
                    AmountMin = currencyInfo.AmountMin,
                    BankNames = currencyInfo.Banks
                };
                return currencyOffer;
            }
        }

        List<CityExchangeOffer> GetOfferCities(ExchangeOffer exchangeOffer, IEnumerable<string> cityNames) {
            var ceo = new List<CityExchangeOffer>();
            foreach (string cityName in cityNames) {
                if (cityDictionary.TryGetValue(cityName, out City city)) {
                    ceo.Add(new CityExchangeOffer() {
                        CityId = city.Id,
                        ExchangeOffer = exchangeOffer
                    });
                }
            }
            return ceo;
        }
    }

}