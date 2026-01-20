using System.Text.Json.Serialization;

namespace StockAlarm.Models;

public sealed class BrapiQuoteResponse
{
    [JsonPropertyName("results")]
    public List<BrapiQuoteResult>? Results { get; set; }
}

public sealed class BrapiQuoteResult
{
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("regularMarketPrice")]
    public decimal? RegularMarketPrice { get; set; }
}
