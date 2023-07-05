using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soanx.OpenAICurrencyExchange.Models;

public class PromptingSet {
    public Message Message { get; set; }
    public FormalizedMessage FormalizedMessage { get; set; }
}

public class FormalizedMessageEx : FormalizedMessage {
    public Message Message { get; set; }
}

public class FormalizedMessage {
    public long Id { get; set; }
    public bool? NotMatched { get; set; }
    public List<Offer> Offers { get; set; }
}

public class Message {
    public long Id { get; set; }
    public string Text { get; set; }
}

public class Offer {
    public List<string> Cities { get; set; }
    public CurrencyInfo Sell { get; set; }
    public CurrencyInfo Buy { get; set; }
    public SellToBuyRate SellToBuyRate { get; set; }
}

public class CurrencyInfo {
    public string Currency { get; set; }
    public decimal? AmountMin { get; set; }
    public decimal? AmountMax { get; set; }
    public List<string> BanksTo { get; set; }

}
public class SellToBuyRate {
    public decimal? RateMin { get; set; }
    public decimal? RateMax { get; set; }
}
