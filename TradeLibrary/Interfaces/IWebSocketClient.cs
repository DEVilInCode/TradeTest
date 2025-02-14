using TradeLibrary.Entities;

namespace TradeLibrary.Interfaces
{
    public interface IWebSocketClient
    {
        event Action<Trade> NewBuyTrade;
        event Action<Trade> NewSellTrade;

        //Deleted param: maxCount. Can't be used for subscription
        void SubscribeTrades(string pair);
        void UnsubscribeTrades(string pair);

        event Action<Candle> CandleSeriesProcessing;

        //Deleted params: from, to, count. Can't be used for subscription
        void SubscribeCandles(string pair, int periodInSec);
        void UnsubscribeCandles(string pair);
    }
}
