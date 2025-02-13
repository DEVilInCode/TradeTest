﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

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
