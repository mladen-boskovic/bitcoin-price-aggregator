using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Text;

namespace Implementation.Caching
{
    public static class PriceCache
    {
        public static string ForPrice(string instrument, DateTime timestampUtc)
        {
            return $"price_{instrument}_{timestampUtc:yyyyMMddHH}";
        }

        public static MemoryCacheEntryOptions DefaultEntryOptions => new()
        {
            Size = 1,
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
            SlidingExpiration = TimeSpan.FromMinutes(30)
        };
    }
}
