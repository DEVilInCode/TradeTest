using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeLibrary.Entities;
using TradeLibrary.General;
using TradeLibrary.Interfaces;

namespace TradeLibrary
{
    public class BitfinexRestClient : IRestClient
    {
        private readonly HttpClient _client = new();

        private readonly HttpRequestMessage _request = new()
        {
            Method = HttpMethod.Get,
            Headers =
                {
                    { "accept", "application/json" },
                },
        };

        private IEnumerable<T> GetSplittedResponse<T>(HttpRequestMessage request, string pair, Func<string[], string, T> stringToT)
        {
            using HttpResponseMessage response = _client.Send(request);
            response.EnsureSuccessStatusCode();

            List<T> list = [];

            //Split into writes
            foreach (string s in response.Content.ReadAsStringAsync().Result[2..^2].Split("],["))
                //Split into fields
                list.Add(stringToT(s.Split(','), pair));

            return list.AsEnumerable();
        }

        public Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount)
        {
            _request.RequestUri = new Uri($"https://api-pub.bitfinex.com/v2/candles/trades/t{pair}/hist?limit={maxCount}");

            return Task.FromResult(GetSplittedResponse(_request, pair, StringParsers.GetTrade));
        }

        public Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, long? count = 0)
        {
            //TODO: use periodInSec, from, to, etc. 
            throw new NotImplementedException();

            _request.RequestUri = new Uri($"https://api-pub.bitfinex.com/v2/candles/trade%3A1m%3At{pair}/hist");

            return Task.FromResult(GetSplittedResponse(_request, pair, StringParsers.GetCandle));
        }
    }
}
