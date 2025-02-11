using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeLibrary.Entities;

namespace TradeLibrary.General
{
    internal static class StringParsers
    {
        public static Trade GetTrade(string[] fields, string pair)
        {
            if (fields.Length != 4)
                throw new ArgumentException("Number of fields must be 4");

            decimal amount = decimal.Parse(fields[2]);
            return new Trade()
            {
                Id = fields[0],
                Time = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(fields[1])),
                Amount = amount,
                Price = decimal.Parse(fields[3]),
                Pair = pair,
                Side = amount > 0 ? "buy" : "sell"
            };
        }

        public static Candle GetCandle(string[] fields, string pair)
        {
            if (fields.Length != 6)
                throw new ArgumentException("Number of fields must be 4");

            return new Candle()
            {
                Pair = pair,
                OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(fields[0])),
                OpenPrice = decimal.Parse(fields[1]),
                ClosePrice = decimal.Parse(fields[2]),
                HighPrice = decimal.Parse(fields[3]),
                LowPrice = decimal.Parse(fields[4]),
                TotalVolume = decimal.Parse(fields[5]),
                TotalPrice = 0 //
            };
        }
    }
}
