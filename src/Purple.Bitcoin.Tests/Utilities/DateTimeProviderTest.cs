using System;
using Purple.Bitcoin.Utilities;
using Purple.Bitcoin.Utilities.Extensions;
using Xunit;

namespace Purple.Bitcoin.Tests.Utilities
{
    public class DateTimeProviderTest
    {
        [Fact]
        public void GetUtcNowReturnsCurrentUtcDateTime()
        {
            var result = DateTimeProvider.Default.GetUtcNow();

            Assert.Equal(DateTime.UtcNow.ToString("yyyyMMddhhmmss"), result.ToString("yyyyMMddhhmmss"));
        }

        [Fact]
        public void GetTimeOffsetReturnsCurrentUtcTimeOffset()
        {
            var result = DateTimeProvider.Default.GetTimeOffset();

            Assert.Equal(DateTimeOffset.UtcNow.ToString("yyyyMMddhhmmss"), result.ToString("yyyyMMddhhmmss"));
        }

        [Fact]
        public void GetTimeReturnsUnixTimeStamp()
        {
            var timeStamp = DateTimeProvider.Default.GetTime();

            Assert.Equal(DateTime.UtcNow.ToUnixTimestamp(), timeStamp);
        }
    }
}
