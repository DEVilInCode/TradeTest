using TradeLibrary.Entities;
using TradeLibrary.Interfaces;

namespace TradeLibrary
{
    //Facade
    public class Connector : IRestClient, IWebSocketClient, IDisposable
    {
        public event Action<Trade>? NewBuyTrade;
        public event Action<Trade>? NewSellTrade;
        public event Action<Candle>? CandleSeriesProcessing;

        private readonly IRestClient _restClient;
        private readonly IWebSocketClient _webSocketClient;
        private bool _disposed = false;

        public Connector(IRestClient restClient, IWebSocketClient webSocketClient)
        {
            _restClient = restClient;
            _webSocketClient = webSocketClient;

            _webSocketClient.NewBuyTrade += OnNewBuyTrade;
            _webSocketClient.NewSellTrade += OnNewSellTrade;
            _webSocketClient.CandleSeriesProcessing += OnCandleSeriesProcessing;
        }
        public async Task<(bool Success, decimal? Price)> TryGetPrice(string from, string to, decimal amount)
        {

            try
            {
                Ticker directTicker = await GetTickerAsync($"{from}{to}");
                if (directTicker.LastPrice != 0)
                    return (true, directTicker.LastPrice * amount);
            }
            catch
            {
                try
                {
                    // Пытаемся получить обратную пару
                    Ticker reverseTicker = await GetTickerAsync($"{to}{from}");
                    if (reverseTicker.LastPrice != 0)
                        return (true, (1m / reverseTicker.LastPrice) * amount);
                }
                catch (Exception)
                {

                    try
                    {
                        // Пытаемся через USD-пары
                        Ticker fromUsdTicker = await GetTickerAsync($"{from}USD");
                        Ticker toUsdTicker = await GetTickerAsync($"{to}USD");

                        if (fromUsdTicker.LastPrice != 0 && toUsdTicker.LastPrice != 0)
                        {
                            decimal crossRate = fromUsdTicker.LastPrice / toUsdTicker.LastPrice;
                            return (true, crossRate * amount);
                        }
                    }
                    catch
                    {
                        return (false, null);
                    }
                }

            }

            return (false, null);

        }

        public Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount)
            => _restClient.GetNewTradesAsync(pair, maxCount);

        public Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, long? count = 0)
            => _restClient.GetCandleSeriesAsync(pair, periodInSec, from, to, count);

        public Task<Ticker> GetTickerAsync(string pair)
            => _restClient.GetTickerAsync(pair);

        public void SubscribeTrades(string pair)
            => _webSocketClient.SubscribeTrades(pair);

        public void UnsubscribeTrades(string pair)
            => _webSocketClient.UnsubscribeTrades(pair);

        public void SubscribeCandles(string pair, int periodInSec)
            => _webSocketClient.SubscribeCandles(pair, periodInSec);

        public void UnsubscribeCandles(string pair)
            => _webSocketClient.UnsubscribeCandles(pair);

        private void OnNewBuyTrade(Trade trade) => NewBuyTrade?.Invoke(trade);

        private void OnNewSellTrade(Trade trade) => NewSellTrade?.Invoke(trade);

        private void OnCandleSeriesProcessing(Candle candle) => CandleSeriesProcessing?.Invoke(candle);

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _webSocketClient.NewBuyTrade -= OnNewBuyTrade;
                    _webSocketClient.NewSellTrade -= OnNewSellTrade;
                    _webSocketClient.CandleSeriesProcessing -= OnCandleSeriesProcessing;
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
