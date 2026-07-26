using ExpensesBackend.API.Domain.DTOs;
using Microsoft.AspNetCore.Http;

namespace ExpensesBackend.API.Services.Interfaces;

public interface IBankSyncService
{
    Task<BankStatementPreviewDto> ParseStatementAsync(
        string connectionId, string userId, IFormFile file, string? password = null);

    Task<BankSyncConfirmResultDto> ConfirmSyncAsync(
        string sessionId, string userId, ConfirmBankSyncRequest request,
        List<string> allowedCategoryIds);
}
