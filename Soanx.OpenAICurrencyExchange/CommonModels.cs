using OpenAI.ObjectModels.SharedModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soanx.CurrencyExchange {
    public class ChatChoiceResult {
        public bool IsSuccess { get; set; }
        public IList<ChatChoiceResponse> Choices { get; set; }
    }
}
