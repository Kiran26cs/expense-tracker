using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Domain.Entities;

namespace ExpensesBackend.API.Services.Interfaces;

public interface IExpenseService
{
    Task<List<ExpenseDto>> GetExpensesAsync(string userId, DateTime? startDate, DateTime? endDate, string? category);
    Task<ExpenseDto> GetExpenseByIdAsync(string userId, string expenseId);
    Task<ExpenseDto> CreateExpenseAsync(string userId, CreateExpenseRequest request);
    Task<ExpenseDto> UpdateExpenseAsync(string userId, string expenseId, UpdateExpenseRequest request);
    Task<bool> DeleteExpenseAsync(string userId, string expenseId);
    Task<string> UploadReceiptAsync(string userId, string expenseId, Stream fileStream, string fileName);
}
