using ExpensesBackend.API.Domain.DTOs;

namespace ExpensesBackend.API.Services.Interfaces;

public interface IAdminCreditService
{
    Task<AdminCreditBalanceDto?> GetBalanceAsync(string bookId);
    Task<AdminCreditBalanceDto> GrantCreditsAsync(string bookId, int amount, string? note, string adminId, string adminEmail);
    Task<AdminCreditTransactionListDto> GetTransactionsAsync(string bookId, int page, int pageSize);
}
