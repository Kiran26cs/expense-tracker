using ExpensesBackend.API.Domain.DTOs;

namespace ExpensesBackend.API.Services.Interfaces;

public interface IExpenseBookService
{
    Task<List<ExpenseBookResponse>> GetExpenseBooksAsync(string userId);
    Task<ExpenseBookResponse> GetExpenseBookByIdAsync(string userId, string expenseBookId);
    Task<ExpenseBookResponse> CreateExpenseBookAsync(string userId, CreateExpenseBookRequest request);
    Task<ExpenseBookResponse> UpdateExpenseBookAsync(string userId, string expenseBookId, UpdateExpenseBookRequest request);
    Task DeleteExpenseBookAsync(string userId, string expenseBookId);
    Task<List<string>> GetExpenseBookCategoriesAsync(string userId);
    Task<ExpenseBookResponse?> GetDefaultExpenseBookAsync(string userId);
    Task UpdateExpenseBookStatsAsync(string expenseBookId);
}
