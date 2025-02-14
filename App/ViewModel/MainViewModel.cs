using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using TradeApp.Model;
using TradeLibrary;
using TradeLibrary.Entities;

namespace TradeApp.ViewModel
{
    public class MainViewModel : IDisposable
    {
        private readonly object _collectionLock = new object();
        private readonly BitfinexWebSocketClient _websocketClient;
        private readonly Connector _connector;
        private bool _disposed;

        public ObservableCollection<TradeModel> Trades { get; } = [];
        public ObservableCollection<CandleModel> Candles { get; } = [];
        public ObservableCollection<Money> Wallet { get; } = [];

        public decimal Balance { get; private set; }

        public MainViewModel()
        {
            _websocketClient = new();
            _connector = new(new BitfinexRestClient(), _websocketClient);

            BindingOperations.EnableCollectionSynchronization(Trades, _collectionLock);

            Money money = new()
            {
                USD = 0,
                BTC = 1,
                XRP = 15000,
                XMR = 50
            };

            Wallet.Add(money);

            Type type = typeof(Money);
            PropertyInfo[] propertyInfos = type.GetProperties();
            string[] names = propertyInfos.Select(x => x.Name).ToArray();

            Money m = new()
            {
                USD = 0,
                BTC = 0,
                XRP = 0,
                XMR = 0
            };

            decimal[] price = new decimal[4];

            for (int i = 0; i < propertyInfos.Length; i++)
            {
                decimal amount = (decimal?)propertyInfos[i].GetValue(money) ?? decimal.Zero;

                if (amount <= decimal.Zero) continue;

                for (int j = 0; j < propertyInfos.Length; j++)
                {
                    if (i == j)
                    {
                        price[j] += amount;
                        continue;
                    }

                    var (Success, Price) = _connector.TryGetPrice(names[i], names[j], amount).Result;
                    if (Success)
                        price[j] += Price ?? decimal.Zero;
                }

            }
            for (int i = 0; i < propertyInfos.Length; i++)
            {
                propertyInfos[i].SetValue(m, price[i]);
            }

            Wallet.Add(m);

            _connector.NewBuyTrade += AddTrade;
            _connector.NewSellTrade += AddTrade;
            _connector.CandleSeriesProcessing += AddCandle;

            _connector.SubscribeTrades("BTCUSD");
            _connector.SubscribeTrades("ETHUSD");
            _connector.SubscribeCandles("BTCUSD", 60);
            _connector.SubscribeCandles("ETHUSD", 300);
        }

        private void AddTrade(Trade trade)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                TradeModel? existing = Trades.FirstOrDefault(t => t.Id == trade.Id);
                if (existing != null)
                    existing.Update(trade);
                else
                    Trades.Add(new TradeModel(trade));
            });
        }

        private void AddCandle(Candle candle)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Candles.Add(new CandleModel(candle));
            });
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _connector.NewBuyTrade -= AddTrade;
                _connector.NewSellTrade -= AddTrade;
                _connector.CandleSeriesProcessing -= AddCandle;

                _connector.Dispose();

                _websocketClient.Dispose();

                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        ~MainViewModel() => Dispose();
    }
}
