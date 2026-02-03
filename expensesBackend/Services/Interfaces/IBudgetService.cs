using ExpensesBackend.API.Domain.Entities;

namespace ExpensesBackend.API.Services.Interfaces;

public interface IBudgetService
{
    Task<List<Budget>> GetBudgetsAsync(string userId, string? month = null);
    Task<Budget?> GetBudgetByIdAsync(string userId, string budgetId);
    Task<Budget> CreateBudgetAsync(Budget budget);
    Task<Budget?> UpdateBudgetAsync(string userId, string budgetId, Budget budget);
    Task<bool> DeleteBudgetAsync(string userId, string budgetId);
}
