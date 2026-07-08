using Application.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    public record PriceResponseDto
    {
        public string Instrument { get; set; } = string.Empty;
        public DateTime TimestampUtc { get; set; }
        public double AggregatedPrice { get; set; }
        public Dictionary<string, double> ProviderPrices { get; set; } = new();
        public PriceSource Source { get; set; }
    }
}
