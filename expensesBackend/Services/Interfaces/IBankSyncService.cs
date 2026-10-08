using ExpensesBackend.API.Domain.DTOs;
using Microsoft.AspNetCore.Http;

namespace ExpensesBackend.API.Services.Interfaces;

public interface IBankSyncService
{
    // expenseBookId is optional for backward compatibility — when supplied, transactions are
    // pre-categorized (payee memory + AI) against that book's categories for the user to review.
    Task<BankStatementPreviewDto> ParseStatementAsync(
        string connectionId, string userId, IFormFile file, string? password = null,
        string? expenseBookId = null);

    Task<BankSyncConfirmResultDto> ConfirmSyncAsync(
        string sessionId, string userId, ConfirmBankSyncRequest request,
        List<string> allowedCategoryIds);
}
