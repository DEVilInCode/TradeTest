using System.Collections.Specialized;
using System.Text.Json;
using System.Web;
using TradeLibrary.Entities;
using TradeLibrary.General;
using TradeLibrary.Interfaces;

namespace TradeLibrary
{
    public class BitfinexRestClient : IRestClient
    {
        private readonly HttpClient _client = new();

        private bool TryGetDeserializedResponse<T>(Uri uri, string pair, out IEnumerable<T>? values)
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

            values = null;

            if (!response.IsSuccessStatusCode)
                return false;

            using JsonDocument doc = JsonDocument.Parse(response.Content.ReadAsStringAsync().Result);
            JsonElement root = doc.RootElement;

            values = JsonArrayDeserialize.ArrayDeserialize<T>(root, pair)
                .Where(x => x is not null)
                .Cast<T>();

            return true;
        }

        public Task<Ticker> GetTickerAsync(string pair)
        {
            TryGetDeserializedResponse<Ticker>(new Uri($"https://api-pub.bitfinex.com/v2/ticker/t{pair}"), pair, out IEnumerable<Ticker>? values);

            if (values == null)
                return (Task<Ticker>)new List<Ticker>() { new() { LastPrice = 0 } }.AsEnumerable();


            return Task.FromResult(values.First());
        }

        public Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount)
        {
            TryGetDeserializedResponse<Trade>(new Uri($"https://api-pub.bitfinex.com/v2/candles/trades/t{pair}/hist?limit={maxCount}"), pair, out IEnumerable<Trade>? values);

            if (values == null)
                throw new HttpRequestException("Wrong request");

            return Task.FromResult(values);
        }

        public Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, long? count = 0)
        {
            NameValueCollection query = HttpUtility.ParseQueryString(string.Empty);
            if (from.HasValue)
                query["start"] = from.Value.ToUnixTimeMilliseconds().ToString();
            if (to.HasValue)
                query["end"] = to.Value.ToUnixTimeMilliseconds().ToString();
            if (count.HasValue && count > 0)
                query["limit"] = Math.Min((long)count, 10000L).ToString();

            UriBuilder builder = new($"https://api-pub.bitfinex.com/v2/candles/trade%3A{TimeFrame.GetTimeFrameFromInt(periodInSec)}%3At{pair}/hist")
            {
                Query = query.ToString()
            };
            Console.WriteLine(builder.Uri.ToString());
            TryGetDeserializedResponse<Candle>(builder.Uri, pair, out IEnumerable<Candle>? values);

            if (values == null)
                throw new HttpRequestException("Wrong request");


            return Task.FromResult(values);
        }
    }
}
