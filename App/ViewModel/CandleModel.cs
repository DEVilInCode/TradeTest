using System.ComponentModel;
using System.Runtime.CompilerServices;
using TradeLibrary.Entities;

namespace TradeApp.ViewModel
{
    public class CandleModel : INotifyPropertyChanged
    {
        private readonly Candle _candle;

        public CandleModel(Candle cadle)
        {
            _candle = cadle;
        }

        public string Pair
        {
            get => _candle.Pair;
        }

        public decimal OpenPrice
        {
            get => _candle.OpenPrice;
        }

        public decimal ClosePrice
        {
            get => _candle.ClosePrice;
        }

        public decimal HighPrice
        {
            get => _candle.HighPrice;
        }

        public decimal LowPrice
        {
            get => _candle.LowPrice;
        }

        public decimal TotalPrice
        {
            get => _candle.TotalPrice;
        }

        public decimal TotalVolume
        {
            get => _candle.TotalVolume;
        }

        public DateTimeOffset OpenTime
        {
            get => _candle.OpenTime;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
