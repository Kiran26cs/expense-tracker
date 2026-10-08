namespace ExpensesBackend.API.Services.Interfaces;

public interface IPayeeCategoryMemoryService
{
    /// <summary>Looks up known category assignments for a set of payee keys within a book.</summary>
    Task<Dictionary<string, (string CategoryId, string CategoryName)>> GetMemoryAsync(
        string expenseBookId, IEnumerable<string> payeeKeys);

    /// <summary>Records (or refreshes) the category a payee should map to for future imports.</summary>
    Task RememberAsync(string expenseBookId, string payeeKey, string categoryId, string categoryName);
}
