using shared;
using Xunit;

namespace tests
{
    public class UnitTest1
    {
        [Theory]
        [InlineData(CountType.CountsPerMinute, 28, 0x00, 0x1c)]
        [InlineData(CountType.CountsPerMinute, 28, 0x10, 0x1c)]
        [InlineData(CountType.CountsPerSecond, 1, 0x80, 0x01)]
        public void Test1(CountType type, int cpm, byte msb, byte lsb)
        {
            Assert.Equal(new(type, cpm), GMCTool.GetCPM(msb, lsb));
        }
    }
}
