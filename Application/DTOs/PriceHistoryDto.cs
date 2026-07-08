using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public class PriceHistoryDto
    {
        public DateTime TimestampUtc { get; set; }
        public double AggregatedPrice { get; set; }
        public Dictionary<string, double> ProviderPrices { get; set; } = new();
    }
}
