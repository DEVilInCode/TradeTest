namespace TradeLibrary.General
{
    public static class TimeFrame
    {
        public static string GetTimeFrameFromInt(int periodInSec)
        {
            return periodInSec switch
            {
                60 => "1m",
                300 => "5m",
                900 => "15m",
                1800 => "30m",
                3600 => "1h",
                10800 => "3h",
                21600 => "6h",
                43200 => "12h",
                86400 => "1D",
                604800 => "1W",
                1209600 => "14D",
                2628000 => "1M",
                _ => throw new ArgumentException("Argument must be round number of allowed time frame"),
            };
        }
    }
}
