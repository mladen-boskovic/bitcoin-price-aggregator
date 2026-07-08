using Application.DTOs;
using Application.Providers;
using System.Text.Json;

namespace Implementation.Providers
{
    public class BitfinexPriceProvider : IPriceProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public string Name => "Bitfinex";

        public BitfinexPriceProvider(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<PriceProviderDto> GetPriceAsync(string instrument, DateTime timestampUtc, CancellationToken cancellationToken = default)
        {
            var start = new DateTimeOffset(DateTime.SpecifyKind(timestampUtc, DateTimeKind.Utc)).ToUnixTimeMilliseconds();

            var end = new DateTimeOffset(DateTime.SpecifyKind(timestampUtc.AddHours(1), DateTimeKind.Utc)).ToUnixTimeMilliseconds();

            var url = $"v2/candles/trade:1h:t{instrument.ToUpper()}/hist?start={start}&end={end}&limit=1";

            var client = _httpClientFactory.CreateClient("Bitfinex");
            var response = await client.GetAsync(url, cancellationToken);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            var data = JsonSerializer.Deserialize<List<List<double>>>(json);

            var candle = data!.FirstOrDefault() ?? throw new InvalidOperationException("Bitfinex returned no data for the requested timestamp.");
            var closePrice = candle[2];

            return new PriceProviderDto
            {
                ProviderName = Name,
                Price = closePrice
            };
        }
    }
}
