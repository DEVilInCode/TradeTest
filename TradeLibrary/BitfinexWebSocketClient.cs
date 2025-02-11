using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TradeLibrary.Entities;
using TradeLibrary.Interfaces;
using TradeLibrary.Jsons;

namespace TradeLibrary
{

    public class BitfinexWebSocketClient : IWebSocketClient, IDisposable
    {
        public event Action<Trade>? NewBuyTrade;
        public event Action<Trade>? NewSellTrade;
        public event Action<Candle>? CandleSeriesProcessing;

        private readonly ClientWebSocket _client;
        private readonly CancellationTokenSource _cancelTokenSource;

        private bool _disposed = false;

        public BitfinexWebSocketClient()
        {
            _client = new();
            _cancelTokenSource = new();

            _client.ConnectAsync(new Uri("wss://api-pub.bitfinex.com/ws/2"), _cancelTokenSource.Token).Wait();

            if (_client.State != WebSocketState.Open)
                throw new WebSocketException("Connection failed");

            Task.Run(() => ListeningAsync(_cancelTokenSource.Token));

        }

        private async Task ListeningAsync(CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[4096];

            try
            {
                while (_client.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result = await _client.ReceiveAsync(buffer, cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", CancellationToken.None);
                        break;
                    }
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine(message);

                    if (!result.EndOfMessage)
                        Console.WriteLine("Message wasn't ended!!!");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        public void SubscribeCandles(string pair, int periodInSec, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = 0)
        {
            CandleSubscriptionMessage message = new()
            {
                Event = "subscribe",
                Channel = "candles",
                Key = $"trade:1m:t{pair}"
            };
            string jsonMessage = JsonSerializer.Serialize(message);
            _client.SendAsync(Encoding.UTF8.GetBytes(jsonMessage), WebSocketMessageType.Text, true, _cancelTokenSource.Token);
        }

        public void SubscribeTrades(string pair, int maxCount = 100)
        {
            TradeSubscriptionMessage message = new()
            {
                Event = "subscribe",
                Channel = "trades",
                Symbol = $"t{pair}"
            };
            string jsonMessage = JsonSerializer.Serialize(message);
            _client.SendAsync(Encoding.UTF8.GetBytes(jsonMessage), WebSocketMessageType.Text, true, _cancelTokenSource.Token);
        }

        public void UnsubscribeCandles(string pair)
        {
            throw new NotImplementedException();
            //CandleSubscriptionMessage message = new()
            //{
            //    Event = "unsubscribe",
            //    Channel = "candles",
            //    Key = $"trade:1m:t{pair}"
            //};
            //string s = JsonSerializer.Serialize(message);
            //_client.SendAsync(Encoding.UTF8.GetBytes(s), WebSocketMessageType.Text, true, _cancelTokenSource.Token);
        }

        public void UnsubscribeTrades(string pair)
        {
            throw new NotImplementedException();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        _client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None).GetAwaiter().GetResult();
                        _cancelTokenSource.Cancel();
                    }
                    finally
                    {
                        _client.Dispose();
                        _cancelTokenSource.Dispose();
                    }
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
