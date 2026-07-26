using ExpensesBackend.API.Domain.DTOs;

namespace ExpensesBackend.API.Services.Interfaces;

public interface IAdminBookService
{
    Task<AdminBookListDto> GetBooksAsync(string? search, bool? aiEnabled, string? userId, int page, int pageSize);
    Task<AdminBookSummaryDto> ToggleAiChatAsync(string bookId, bool enabled, string adminId, string adminEmail);
    Task DeleteBookAsync(string bookId, string adminId, string adminEmail);
}
