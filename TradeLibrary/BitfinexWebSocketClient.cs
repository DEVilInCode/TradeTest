using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using TradeLibrary.Entities;
using TradeLibrary.General;
using TradeLibrary.Interfaces;

namespace TradeLibrary
{

    public class BitfinexWebSocketClient : IWebSocketClient, IDisposable
    {
        public event Action<Trade>? NewBuyTrade;
        public event Action<Trade>? NewSellTrade;
        public event Action<Candle>? CandleSeriesProcessing;

        private readonly ClientWebSocket _client;
        private readonly CancellationTokenSource _cancelTokenSource;

        private readonly ConcurrentDictionary<(string Channel, string Pair), string> _activeSubscriptions = [];

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
                    using MemoryStream messageStream = new();
                    WebSocketReceiveResult result;

                    do
                    {
                        result = await _client.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                        if (result.MessageType != WebSocketMessageType.Text)
                        {
                            if (result.MessageType == WebSocketMessageType.Close)
                                await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", CancellationToken.None);
                            else
                                await _client.CloseAsync(WebSocketCloseStatus.InvalidMessageType, "Unexpected message type", CancellationToken.None);
                            return;
                        }

                        messageStream.Write(buffer, 0, result.Count);

                    } while (!result.EndOfMessage);

                    string message = Encoding.UTF8.GetString(messageStream.ToArray());

                    Console.WriteLine(message);

                    if (!string.IsNullOrEmpty(message))
                        await Task.Run(() => JsonHandler(message), _cancelTokenSource.Token);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }


        private void JsonHandler(string message)
        {
            using JsonDocument jsonDocument = JsonDocument.Parse(message);
            JsonElement root = jsonDocument.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("event", out JsonElement element))
                    HandleEvent(element.GetString() ?? string.Empty, root);
            }
            else if (root.ValueKind == JsonValueKind.Array)
            {
                string chanId = root[0].ToString();
                var (channel, pair) = _activeSubscriptions.First(x => x.Value == chanId).Key;

                if (root[1].ValueKind == JsonValueKind.String)
                {
                    string s = root[1].GetString() ?? string.Empty;

                    if (s.SequenceEqual("te"))
                    {
                        HandleChannelArray(channel, root[2], pair);
                    }
                    else if (s.SequenceEqual("tu"))
                    {
                        HandleChannelArray(channel, root[2], pair);
                    }
                }
                else if (root[1].ValueKind == JsonValueKind.Array)
                {
                    HandleChannelArray(channel, root[1], pair);
                }
            }
        }

        private void HandleChannelArray(string channel, JsonElement arrayElement, string pair)
        {
            switch (channel)
            {
                case "trades":
                    JsonArrayDeserialize.ArrayDeserialize<Trade>(arrayElement, pair).ToList().ForEach(e =>
                    {
                        ArgumentNullException.ThrowIfNull(e, nameof(e));

                        if (e.Side.SequenceEqual("buy"))
                            NewBuyTrade?.Invoke(e);
                        else
                            NewSellTrade?.Invoke(e);
                    });
                    break;
                case "candles":
                    JsonArrayDeserialize.ArrayDeserialize<Candle>(arrayElement, pair).ToList().ForEach(e =>
                    {
                        ArgumentNullException.ThrowIfNull(e, nameof(e));

                        CandleSeriesProcessing?.Invoke(e);
                    });
                    break;
                default:
                    break;
            };
        }

        private void HandleEvent(string eventName, JsonElement root)
        {
            switch (eventName)
            {
                case "subscribed":
                    if (root.TryGetProperty("channel", out JsonElement channelElement) &&
                        root.TryGetProperty("chanId", out JsonElement chanIdElement))
                    {
                        string channel = channelElement.GetString() ?? throw new InvalidOperationException("Missing 'channel' value");
                        string chanId = chanIdElement.GetInt64().ToString() ?? throw new InvalidOperationException("Missing 'chanId' value");

                        string pair = string.Empty;

                        if (root.TryGetProperty("pair", out JsonElement pairElement))
                        {
                            //trades
                            pair = pairElement.GetString() ?? string.Empty;
                        }
                        else if (root.TryGetProperty("key", out JsonElement keyElement))
                        {
                            //candles
                            string rawKey = keyElement.GetString() ?? string.Empty;
                            int index = rawKey.LastIndexOf(":t", StringComparison.Ordinal) + 2;

                            pair = index >= 0 ? rawKey[index..] : rawKey;
                        }

                        if (!string.IsNullOrEmpty(pair))
                        {
                            _activeSubscriptions.TryAdd((channel, pair), chanId);
                        }

                    }
                    break;

                default:
                    break;
            }
        }

        public void SubscribeCandles(string pair, int periodInSec)
        {
            var message = new
            {
                @event = "subscribe",
                channel = "candles",
                key = $"trade:{TimeFrame.GetTimeFrameFromInt(periodInSec)}:t{pair}"
            };

            Send(message);
        }

        public void SubscribeTrades(string pair)
        {
            var message = new
            {
                @event = "subscribe",
                channel = "trades",
                symbol = $"t{pair}"
            };

            Send(message);
        }

        private void Send<T>(T message)
        {
            string jsonMessage = JsonSerializer.Serialize(message);
            _client.SendAsync(Encoding.UTF8.GetBytes(jsonMessage), WebSocketMessageType.Text, true, _cancelTokenSource.Token);
        }

        public void UnsubscribeCandles(string pair)
        {
            Unsubscribe(pair, "candles");
        }

        public void UnsubscribeTrades(string pair)
        {
            Unsubscribe(pair, "trades");
        }

        private void Unsubscribe(string pair, string channel)
        {
            if (_activeSubscriptions.TryRemove((channel, pair), out string? id))
            {
                var message = new
                {
                    @event = "unsubscribe",
                    chanId = id
                };
                string jsonMessage = JsonSerializer.Serialize(message);
                _client.SendAsync(Encoding.UTF8.GetBytes(jsonMessage), WebSocketMessageType.Text, true, _cancelTokenSource.Token).Wait();
            }
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
