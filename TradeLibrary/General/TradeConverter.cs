using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TradeLibrary.Entities;

namespace TradeLibrary.General
{
    internal class TradeConverter : JsonConverter<Trade>
    {
        public override Trade Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
        {
            var trade = new Trade();
            int index = 0;

            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException("Expected array start");

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                switch (index)
                {
                    case 0: // Id
                        trade.Id = reader.GetInt64().ToString();
                        break;
                    case 1: // Time
                        trade.Time = DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64());
                        break;
                    case 2: // Amount
                        trade.Amount = reader.GetDecimal();
                        break;
                    case 3: // Price
                        trade.Price = reader.GetDecimal();
                        break;
                }
                index++;
            }

            return trade;
        }

        public override void Write(
            Utf8JsonWriter writer,
            Trade trade,
            JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteStringValue(trade.Id);
            writer.WriteNumberValue(trade.Time.ToUnixTimeMilliseconds());
            writer.WriteNumberValue(trade.Amount);
            writer.WriteNumberValue(trade.Price);
            writer.WriteEndArray();
        }
    }
}
