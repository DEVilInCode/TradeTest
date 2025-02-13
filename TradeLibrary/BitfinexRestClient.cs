using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TradeLibrary.Entities;
using TradeLibrary.General;
using TradeLibrary.Interfaces;

namespace TradeLibrary
{
    public class BitfinexRestClient : IRestClient
    {
        private readonly HttpClient _client = new();

        private IEnumerable<T> GetDeserializedResponse<T>(Uri uri, string pair)
        {
            HttpRequestMessage request = new()
            {
                Method = HttpMethod.Get,
                Headers =
                {
                    { "accept", "application/json" },
                },
                RequestUri = uri
            };

            using HttpResponseMessage response = _client.Send(request);
            response.EnsureSuccessStatusCode();

            using JsonDocument doc = JsonDocument.Parse(response.Content.ReadAsStringAsync().Result);
            JsonElement root = doc.RootElement;

            return JsonArrayDeserialize.ArrayDeserialize<T>(root, pair)
                .Where(x => x is not null)
                .Cast<T>();
        }

        public Task<Ticker> GetTickerAsync(string pair)
        {
            return Task.FromResult(
                GetDeserializedResponse<Ticker>(new Uri($"https://api-pub.bitfinex.com/v2/ticker/t{pair}"), pair).First());
        }

        public Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount)
        {
            return Task.FromResult(
                GetDeserializedResponse<Trade>(new Uri($"https://api-pub.bitfinex.com/v2/candles/trades/t{pair}/hist?limit={maxCount}"), pair));
        }

        public Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, long? count = 0)
        {
            return Task.FromResult(GetDeserializedResponse<Candle>(
                new Uri($"https://api-pub.bitfinex.com/v2/candles/trade%3A" +
                $"{TimeFrame.GetTimeFrameFromInt(periodInSec)}%3At{pair}/hist{(count > 0 ? $"?limit={count}" : string.Empty)}"), pair));
        }
    }
}
