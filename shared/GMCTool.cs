namespace shared
{
    public static class GMCTool
    {
        public static Counts GetCPM(byte msb, byte lsb)
        {
            var v = (lsb | msb >> 8) & 0b0011111111111111;
            var t = msb >> 7;
            return new((CountType)t, v);
        }
    }

    public enum CountType : byte
    {
        CountsPerMinute,
        CountsPerSecond
    }

    public record Counts(CountType Type, int Value);
}
