using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
                        JsonHandler(message);
                    
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        private static List<T?> ArrayDeserialize<T>(JsonElement.ArrayEnumerator elements)
        {
            List<T?> list = [];
            JsonSerializerOptions options = new()
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new CandleConverter(), new TradeConverter() }
            };

            return elements.Select(e => JsonSerializer.Deserialize<T>(e, options)).ToList();
        }

        private void JsonHandler(string message)
        {
            using JsonDocument jsonDocument = JsonDocument.Parse(message);
            JsonElement root = jsonDocument.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                List<JsonElement> list = [.. root.EnumerateArray()];
                string chanId = list[0].ToString();
                var (channel, pair) = _activeSubscriptions.First(x => x.Value == chanId).Key;
                
                switch (channel)
                {
                    case "trades":
                        ArrayDeserialize<Trade>(list[1].EnumerateArray()).ForEach(e =>
                        {
                            e.Side = e.Amount > 0 ? "buy" : "sell";
                            e.Pair = pair;
                            if (e.Side.SequenceEqual("buy"))
                                NewBuyTrade?.Invoke(e);
                            else
                                NewSellTrade?.Invoke(e);
                        });
                        break;
                    case "candles":
                        ArrayDeserialize<Candle>(list[1].EnumerateArray()).ForEach(e =>
                        {
                            e.TotalPrice = 0;
                            e.Pair = pair;
                            CandleSeriesProcessing?.Invoke(e);
                        });
                        break;
                    default:
                        break;
                };


            }
            else if (root.TryGetProperty("event", out JsonElement element))
            {
                switch (element.GetString())
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
        }

        public void SubscribeCandles(string pair, int periodInSec, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = 0)
        {
            var message = new
            {
                @event = "subscribe",
                channel = "candles",
                key = $"trade:{TimeFrame.GetTimeFrameFromInt(periodInSec)}:t{pair}"
            };

            Send(message);
        }

        public void SubscribeTrades(string pair, int maxCount = 100)
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
