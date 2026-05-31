namespace ExpensesBackend.API.Services.Interfaces;

public interface ICategoryClassifier
{
    /// <summary>
    /// Classifies a category name as "need", "want", "debt", or null (unclassified/unknown).
    /// Never throws — returns null on any failure.
    /// </summary>
    Task<string?> ClassifyAsync(string categoryName);
}
