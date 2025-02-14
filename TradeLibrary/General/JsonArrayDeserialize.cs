using System.Text.Json;
using System.Text.Json.Serialization;

namespace TradeLibrary.General
{
    internal static class JsonArrayDeserialize
    {
        public static IEnumerable<T?> ArrayDeserialize<T>(JsonElement element, string pair)
        {
            JsonSerializerOptions options = new()
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new CandleConverter(pair), new TradeConverter(pair), new TickerConverter(pair) }
            };

            JsonElement.ArrayEnumerator arrayEnum = element.EnumerateArray();

            if (arrayEnum.First().ValueKind == JsonValueKind.Array)
                return arrayEnum.Select(e => e.Deserialize<T>(options));
            else
                return [element.Deserialize<T>(options)];
        }
    }
}
