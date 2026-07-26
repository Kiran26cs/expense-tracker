namespace ExpensesBackend.API.Services.Interfaces;

public interface IBankTransactionCategorizer
{
    /// <summary>
    /// Classifies a batch of bank transaction descriptions into category names.
    /// Returns a mapping of rowNumber → categoryName.
    /// Always returns a value for every row — falls back to "Uncategorized" if AI is unavailable.
    /// Never throws.
    /// </summary>
    Task<Dictionary<int, string>> ClassifyAsync(
        List<(int RowNumber, string Description)> transactions,
        List<string> availableCategoryNames);
}
