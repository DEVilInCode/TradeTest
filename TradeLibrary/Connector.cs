using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
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

        public Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount)
            => _restClient.GetNewTradesAsync(pair, maxCount);

        public Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, long? count = 0)
            => _restClient.GetCandleSeriesAsync(pair, periodInSec, from, to, count);

        public void SubscribeTrades(string pair, int maxCount = 100)
            => _webSocketClient.SubscribeTrades(pair, maxCount);

        public void UnsubscribeTrades(string pair)
            => _webSocketClient.UnsubscribeTrades(pair);

        public void SubscribeCandles(string pair, int periodInSec, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = 0)
            => _webSocketClient.SubscribeCandles(pair, periodInSec, from, to, count);

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
