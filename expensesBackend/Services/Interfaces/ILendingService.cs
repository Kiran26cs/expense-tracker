using ExpensesBackend.API.Domain.DTOs;

namespace ExpensesBackend.API.Services.Interfaces;

public interface ILendingService
{
    Task<List<LendingDto>> GetLendingsAsync(string userId, string expenseBookId, string? status = null);
    Task<LendingDto> GetLendingByIdAsync(string userId, string expenseBookId, string lendingId);
    Task<LendingDto> CreateLendingAsync(string userId, CreateLendingRequest request);
    Task<LendingDto> UpdateLendingAsync(string userId, string expenseBookId, string lendingId, UpdateLendingRequest request);
    Task DeleteLendingAsync(string userId, string expenseBookId, string lendingId);
    Task SettleLendingAsync(string userId, string expenseBookId, string lendingId, decimal? interestCollected, DateTime? settlementDate, string? notes);

    Task<LendingRepaymentsResponse> GetRepaymentsAsync(string userId, string expenseBookId, string lendingId, int page, int pageSize);
    Task<RepaymentDto> AddRepaymentAsync(string userId, string expenseBookId, string lendingId, CreateRepaymentRequest request);
    Task DeleteRepaymentAsync(string userId, string expenseBookId, string lendingId, string repaymentId);
}
