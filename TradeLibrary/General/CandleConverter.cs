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
    internal class CandleConverter : JsonConverter<Candle>
    {
        public override Candle Read(
       ref Utf8JsonReader reader,
       Type typeToConvert,
       JsonSerializerOptions options)
        {
            var candle = new Candle();
            int index = 0;

            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException("Expected array start");

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                switch (index)
                {
                    case 0: // OpenTime
                        candle.OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64());
                        break;
                    case 1: // OpenPrice
                        candle.OpenPrice = reader.GetDecimal();
                        break;
                    case 2: // ClosePrice
                        candle.ClosePrice = reader.GetDecimal();
                        break;
                    case 3: // HighPrice
                        candle.HighPrice = reader.GetDecimal();
                        break;
                    case 4: // LowPrice
                        candle.LowPrice = reader.GetDecimal();
                        break;
                    case 5: // TotalVolume
                        candle.TotalVolume = reader.GetDecimal();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
                index++;
            }

            return candle;
        }

        public override void Write(
            Utf8JsonWriter writer,
            Candle candle,
            JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteStringValue(candle.OpenTime);
            writer.WriteNumberValue(candle.OpenPrice);
            writer.WriteNumberValue(candle.ClosePrice);
            writer.WriteNumberValue(candle.HighPrice);
            writer.WriteNumberValue(candle.LowPrice);
            writer.WriteNumberValue(candle.TotalVolume);
            writer.WriteEndArray();
        }
    }
}
