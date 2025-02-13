using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeLibrary.Entities;

namespace TradeLibrary.Interfaces
{
    public interface IRestClient
    {
        Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount);
        Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, long? count = 0);

        //Add GetTicker method to have access to pair last prices
        Task<Ticker> GetTickerAsync(string pair);

    }
}
