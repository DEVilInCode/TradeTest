using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TradeLibrary.Jsons
{
    internal class CandleSubscriptionMessage
    {
        [JsonPropertyName("event")]
        public string Event { get; set; }

        [JsonPropertyName("channel")]
        public string Channel { get; set; }

        [JsonPropertyName("key")]
        public string Key { get; set; }
    }
}
