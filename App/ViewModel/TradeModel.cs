using System.ComponentModel;
using System.Runtime.CompilerServices;
using TradeLibrary.Entities;

namespace TradeApp.ViewModel
{
    public class TradeModel : INotifyPropertyChanged
    {
        private readonly Trade _trade;

        public TradeModel(Trade trade)
        {
            _trade = trade;
        }

        public void Update(Trade trade)
        {
            _trade.Price = trade.Price;
            _trade.Amount = trade.Amount;
            _trade.Side = trade.Side;
            _trade.Time = trade.Time;
            OnPropertyChanged();
        }

        public string Pair
        {
            get => _trade.Pair;
        }

        public decimal Price
        {
            get => _trade.Price;
        }

        public decimal Amount
        {
            get => _trade.Amount;
        }

        public string Side
        {
            get => _trade.Side;
        }

        public string Id
        {
            get => _trade.Id;
        }

        public DateTimeOffset Time
        {
            get => _trade.Time;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
