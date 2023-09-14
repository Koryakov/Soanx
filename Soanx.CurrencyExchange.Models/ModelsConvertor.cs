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
            var exchangeOffer = new EfModels.ExchangeOffer();
            exchangeOffer.TgMessageId = dtoMessage.Id;

            foreach (Offer dtoOffer in dtoMessage.Offers) {
                
                exchangeOffer.SellCurrencyOffer = new CurrencyOffer() {
                    ExchangeTypeId = ExchangeType.Sell,
                    AmountMax = dtoOffer.Sell.AmountMax,
                    AmountMin = dtoOffer.Sell.AmountMin,
                    BankNames = dtoOffer.Sell.Banks
                };
                exchangeOffer.BuyCurrencyOffer= new CurrencyOffer() {
                    ExchangeTypeId = ExchangeType.Buy,
                    AmountMax = dtoOffer.Buy.AmountMax,
                    AmountMin = dtoOffer.Buy.AmountMin,
                    BankNames = dtoOffer.Buy.Banks
                };

                exchangeOffer.RateMax = dtoOffer.RateMax;
                exchangeOffer.RateMin = dtoOffer.RateMin;

                 var ceo = new List<CityExchangeOffer>();
                //TODO: Parse the City list to entities. Need to clarify string format for cities.
                exchangeOffer.CityExchangeOffers = ceo;

                var cityNames = Enumerable.Empty<string>();
                if (dtoOffer.Cities?.Count() > 0) {
                    cityNames = dtoOffer.Cities;
                } else if (dtoMessage.Cities?.Count() > 0) {
                    cityNames = dtoMessage.Cities;
                }
                exchangeOffer.CityExchangeOffers = GetOfferCities(exchangeOffer, cityNames);
            }
            exchangeOfferList.Add(exchangeOffer);
        }
        return exchangeOfferList;

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

    private void InitCities(Dictionary<string, EfModels.City> cityDictionary) {

    }
}