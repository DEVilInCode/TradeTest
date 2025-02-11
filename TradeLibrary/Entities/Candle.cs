using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TradeLibrary.Entities
{
    public class Candle
    {
        /// <summary>
        /// Валютная пара
        /// </summary>
        [JsonIgnore]
        public string Pair { get; set; }

        /// <summary>
        /// Цена открытия
        /// </summary>
        [JsonPropertyOrder(1)]
        public decimal OpenPrice { get; set; }

        /// <summary>
        /// Максимальная цена
        /// </summary>
        [JsonPropertyOrder(3)]
        public decimal HighPrice { get; set; }

        /// <summary>
        /// Минимальная цена
        /// </summary>
        [JsonPropertyOrder(4)]
        public decimal LowPrice { get; set; }

        /// <summary>
        /// Цена закрытия
        /// </summary>
        [JsonPropertyOrder(2)]
        public decimal ClosePrice { get; set; }


        /// <summary>
        /// Partial (Общая сумма сделок)
        /// </summary>
        [JsonIgnore]
        public decimal TotalPrice { get; set; }

        /// <summary>
        /// Partial (Общий объем)
        /// </summary>
        [JsonPropertyOrder(5)]
        public decimal TotalVolume { get; set; }

        /// <summary>
        /// Время
        /// </summary>
        [JsonPropertyOrder(0)]
        public DateTimeOffset OpenTime { get; set; }

    }
}
