using Application.DTOs;
using Application.Providers;
using Implementation.Providers.DTOs;
using System.Globalization;
using System.Text.Json;

namespace Implementation.Providers
{
    public class BitstampPriceProvider : IPriceProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public string Name => "Bitstamp";

        public BitstampPriceProvider(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<PriceProviderDto> GetPriceAsync(string instrument, DateTime timestampUtc, CancellationToken cancellationToken = default)
        {
            var unixTime = new DateTimeOffset(DateTime.SpecifyKind(timestampUtc, DateTimeKind.Utc)).ToUnixTimeSeconds();

            var url = $"api/v2/ohlc/{instrument.ToLower()}/?step=3600&limit=1&start={unixTime}";

            var client = _httpClientFactory.CreateClient("Bitstamp");
            var response = await client.GetAsync(url, cancellationToken);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<BitstampResponse>(json);

            var candle = result!.Data.Ohlc.FirstOrDefault() ?? throw new InvalidOperationException("Bitstamp returned no data for the requested timestamp.");
            var closePrice = double.Parse(candle.Close, CultureInfo.InvariantCulture);

            return new PriceProviderDto
            {
                ProviderName = Name,
                Price = closePrice
            };
        }
    }
}
