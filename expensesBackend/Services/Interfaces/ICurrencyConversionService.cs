namespace ExpensesBackend.API.Services.Interfaces;

public interface ICurrencyConversionService
{
    /// <summary>Returns the rate to convert 1 unit of <paramref name="from"/> into <paramref name="to"/>
    /// as of the given date, or null if the pair is unsupported.</summary>
    Task<decimal?> GetRateAsync(string from, string to, DateTime date);
}
