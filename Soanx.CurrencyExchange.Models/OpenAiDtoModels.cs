﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soanx.CurrencyExchange.OpenAiDtoModels;

public class PromptingSet {
    public MessageForAnalyzing Message { get; set; }
    public FormalizedMessage FormalizedMessage { get; set; }
}

public class FormalizedMessageEx : FormalizedMessage {
    public MessageForAnalyzing Message { get; set; }
}

public class FormalizedMessage {
    public long Id { get; set; }
    public bool? NotMatched { get; set; }
    public List<string>? Cities { get; set; }
    public List<Offer> Offers { get; set; }
}

public class MessageForAnalyzing {
    public long Id { get; set; }
    public string Text { get; set; }
}

public class Offer {
    public List<string>? Cities { get; set; }
    public CurrencyInfo Sell { get; set; }
    public CurrencyInfo Buy { get; set; }
    public SellToBuyRate SellToBuyRate { get; set; }
}

public class CurrencyInfo {
    public List<string> Currencies { get; set; }
    public decimal? AmountMin { get; set; }
    public decimal? AmountMax { get; set; }
    public List<string> BanksTo { get; set; }

}
public class SellToBuyRate {
    public decimal? RateMin { get; set; }
    public decimal? RateMax { get; set; }
}