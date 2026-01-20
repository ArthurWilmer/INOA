using System.Net.Http.Headers;
using System.Text.Json;
using StockAlarm.Models;

namespace StockAlarm.Services;

public sealed class QuoteService
{
    private readonly HttpClient _httpClient;

    public QuoteService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<decimal> GetRegularMarketPriceAsync(string ticker, string? brapiToken, CancellationToken ct)
    {
        // Ex.: https://brapi.dev/api/quote/PETR4
        var url = $"https://brapi.dev/api/quote/{Uri.EscapeDataString(ticker)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        if (!string.IsNullOrWhiteSpace(brapiToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", brapiToken);

        using var response = await _httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"BRAPI retornou erro: {(int)response.StatusCode} {response.ReasonPhrase}");

        var json = await response.Content.ReadAsStringAsync(ct);

        var data = JsonSerializer.Deserialize<BrapiQuoteResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var price = data?.Results?.FirstOrDefault()?.RegularMarketPrice;

        if (price is null)
            throw new InvalidOperationException("Não foi possível obter regularMarketPrice da BRAPI (resposta inesperada).");

        return price.Value;
    }
}
