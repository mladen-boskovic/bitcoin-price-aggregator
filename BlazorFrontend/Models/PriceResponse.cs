namespace BlazorFrontend.Models
{
    public class PriceResponse
    {
        public string Instrument { get; set; } = string.Empty;
        public DateTime TimestampUtc { get; set; }
        public double AggregatedPrice { get; set; }
        public Dictionary<string, double> ProviderPrices { get; set; } = new();
        public string Source { get; set; } = string.Empty;
    }
}
