using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeLibrary.Entities;

namespace TradeLibrary.Interfaces
{
    public interface IWebSocketClient
    {
        event Action<Trade> NewBuyTrade;
        event Action<Trade> NewSellTrade;
        void SubscribeTrades(string pair, int maxCount = 100);
        void UnsubscribeTrades(string pair);

        event Action<Candle> CandleSeriesProcessing;
        void SubscribeCandles(string pair, int periodInSec, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = 0);
        void UnsubscribeCandles(string pair);
    }
}
