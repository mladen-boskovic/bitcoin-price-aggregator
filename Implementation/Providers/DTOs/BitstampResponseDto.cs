using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Implementation.Providers.DTOs
{
    public class BitstampResponse
    {
        [JsonPropertyName("data")]
        public BitstampData Data { get; set; } = new();
    }

    public class BitstampData
    {
        [JsonPropertyName("ohlc")]
        public List<BitstampCandle> Ohlc { get; set; } = [];
    }

    public class BitstampCandle
    {
        [JsonPropertyName("close")]
        public string Close { get; set; } = string.Empty;
    }
}
