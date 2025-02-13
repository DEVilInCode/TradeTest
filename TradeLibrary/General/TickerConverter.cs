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
    internal class TickerConverter : JsonConverter<Ticker>
    {
        private string _pair;
        public TickerConverter(string pair)
        {
            _pair = pair;
        }

        public override Ticker Read(
      ref Utf8JsonReader reader,
      Type typeToConvert,
      JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException("Expected array start");

            Ticker ticker = new()
            {
                Pair = _pair
            };
            int index = 0;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                switch (index)
                {
                    case 0:
                        ticker.Bid = reader.GetDecimal();
                        break;
                    case 1:
                        ticker.BidSize = reader.GetDecimal();
                        break;
                    case 2:
                        ticker.Ask = reader.GetDecimal();
                        break;
                    case 3:
                        ticker.AskSize = reader.GetDecimal();
                        break;
                    case 4:
                        ticker.DailyChange = reader.GetDecimal();
                        break;
                    case 5:
                        ticker.DailyChangeRelative = reader.GetDecimal();
                        break;
                    case 6:
                        ticker.LastPrice = reader.GetDecimal();
                        break;
                    case 7:
                        ticker.Volume = reader.GetDecimal();
                        break;
                    case 8:
                        ticker.High = reader.GetDecimal();
                        break;
                    case 9:
                        ticker.Low = reader.GetDecimal();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
                index++;
            }

            return ticker;
        }

        public override void Write(
            Utf8JsonWriter writer,
            Ticker ticker,
            JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(ticker.Bid);
            writer.WriteNumberValue(ticker.BidSize);
            writer.WriteNumberValue(ticker.Ask);
            writer.WriteNumberValue(ticker.AskSize);
            writer.WriteNumberValue(ticker.DailyChange);
            writer.WriteNumberValue(ticker.DailyChangeRelative);
            writer.WriteNumberValue(ticker.LastPrice);
            writer.WriteNumberValue(ticker.Volume);
            writer.WriteNumberValue(ticker.High);
            writer.WriteNumberValue(ticker.Low);
            writer.WriteEndArray();
        }
    }
}
