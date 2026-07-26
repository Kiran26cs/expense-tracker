using ExpensesBackend.API.Domain.DTOs;

namespace ExpensesBackend.API.Services.Interfaces;

public interface IBankConnectionService
{
    Task<List<BankConnectionDto>> GetConnectionsAsync(string userId);
    Task<BankConnectionDto> GetConnectionAsync(string connectionId, string userId);
    Task<BankConnectionDto> CreateConnectionAsync(string userId, CreateBankConnectionRequest request);
    Task<BankConnectionDto> UpdateConnectionAsync(string connectionId, string userId, UpdateBankConnectionRequest request);
    Task DeleteConnectionAsync(string connectionId, string userId);

    // Book ↔ connection mapping
    Task<BookBankConnectionDto> GetBookConnectionAsync(string expenseBookId, string userId);
    Task MapToBookAsync(string expenseBookId, string userId, string? bankConnectionId);
}
