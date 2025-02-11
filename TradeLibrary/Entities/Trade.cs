using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TradeLibrary.Entities
{
    public class Trade
    {
        /// <summary>
        /// Валютная пара
        /// </summary>
        [JsonIgnore]
        public string Pair { get; set; }

        /// <summary>
        /// Цена трейда
        /// </summary>
        [JsonPropertyOrder(3)]
        public decimal Price { get; set; }

        /// <summary>
        /// Объем трейда
        /// </summary>
        [JsonPropertyOrder(2)]
        public decimal Amount { get; set; }

        /// <summary>
        /// Направление (buy/sell)
        /// </summary>
        [JsonIgnore]
        public string Side { get; set; }

        /// <summary>
        /// Время трейда
        /// </summary>
        [JsonPropertyOrder(1)]
        public DateTimeOffset Time { get; set; }


        /// <summary>
        /// Id трейда
        /// </summary>
        [JsonPropertyOrder(0)]
        public string Id { get; set; }

    }
}
