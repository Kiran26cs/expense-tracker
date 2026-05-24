using ExpensesBackend.API.Services.Interfaces;
using System.Text.Json;

namespace ExpensesBackend.API.Services;

public class FrankfurterCurrencyService : ICurrencyConversionService
{
    private readonly HttpClient _http;
    private readonly ILogger<FrankfurterCurrencyService> _logger;

    public FrankfurterCurrencyService(HttpClient http, ILogger<FrankfurterCurrencyService> logger)
    {
        _http   = http;
        _logger = logger;
    }

    public async Task<decimal?> GetRateAsync(string from, string to, DateTime date)
    {
        from = from.ToUpperInvariant();
        to   = to.ToUpperInvariant();

        if (from == to) return 1m;

        var dateStr = date.Date <= DateTime.UtcNow.Date
            ? date.Date.ToString("yyyy-MM-dd")
            : "latest";

        var url = $"https://api.frankfurter.app/{dateStr}?from={from}&to={to}";

        try
        {
            var json = await _http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("rates", out var rates) &&
                rates.TryGetProperty(to, out var rateEl))
            {
                return (decimal)rateEl.GetDouble();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Frankfurter rate lookup failed for {From}/{To} on {Date}: {Msg}",
                from, to, dateStr, ex.Message);
        }

        return null;
    }
}
