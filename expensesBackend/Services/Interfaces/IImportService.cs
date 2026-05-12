using ExpensesBackend.API.Domain.DTOs;

namespace ExpensesBackend.API.Services.Interfaces;

public interface IImportService
{
    Task<ImportSessionDto> CreateImportSessionAsync(
        string expenseBookId, string userId, StartImportRequest request,
        List<string> allowedCategoryIds);

    Task<List<ImportSessionSummaryDto>> GetImportSessionsAsync(string expenseBookId, string userId);

    Task<ImportSessionDto> GetImportSessionByIdAsync(string importId, string expenseBookId, string userId);

    Task<ImportSessionDto> RetryFailedAsync(
        string importId, string expenseBookId, string userId,
        List<string> allowedCategoryIds, List<int>? rowNumbers = null);
}
