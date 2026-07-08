using BlazorFrontend.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace BlazorFrontend.Services
{
    public class PriceApiService
    {
        private readonly HttpClient _http;

        public PriceApiService(HttpClient http)
        {
            _http = http;
        }

        public async Task<PriceResponse?> GetAggregatedPriceAsync(string instrument, DateTime timestampUtc)
        {
            var ts = Uri.EscapeDataString(timestampUtc.ToString("o"));
            var url = $"api/price?instrument={Uri.EscapeDataString(instrument)}&timestampUtc={ts}";

            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(ExtractMessage(errorBody), null, response.StatusCode);
            }

            return await response.Content.ReadFromJsonAsync<PriceResponse>();
        }

        private static string ExtractMessage(string body)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                if (root.TryGetProperty("errors", out var errors))
                {
                    var messages = new List<string>();

                    foreach (var field in errors.EnumerateObject())
                    {
                        foreach (var message in field.Value.EnumerateArray())
                        {
                            messages.Add(message.GetString() ?? string.Empty);
                        }
                    }

                    if (messages.Count > 0)
                    {
                        return string.Join("; ", messages);
                    }
                }

                if (root.TryGetProperty("title", out var title))
                {
                    if (root.TryGetProperty("detail", out var detail))
                    {
                        var detailStr = detail.GetString();
                        if (!string.IsNullOrEmpty(detailStr))
                            return detailStr;
                    }
                    return title.GetString() ?? body;
                }
            }
            catch { }

            return body;
        }
    }
}