using Implementation.Caching;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tests.Caching
{
    public class PriceCacheTests
    {
        [Fact]
        public void ForPrice_ReturnsCorrectFormat()
        {
            var timestamp = new DateTime(2024, 6, 15, 14, 30, 0, DateTimeKind.Utc);
            var key = PriceCache.ForPrice("BTCUSD", timestamp);
            Assert.Equal("price_BTCUSD_2024061514", key);
        }
    }
}
