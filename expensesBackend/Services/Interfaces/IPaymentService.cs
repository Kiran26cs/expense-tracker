using ExpensesBackend.API.Domain.DTOs;

namespace ExpensesBackend.API.Services.Interfaces;

public interface IPaymentService
{
    Task<CreateSubscriptionResponseDto> CreateSubscriptionAsync(string userId, string plan);
    Task<SubscriptionStatusDto> VerifyAndActivateAsync(string userId, VerifyPaymentRequestDto request);
    Task<SubscriptionStatusDto?> GetStatusAsync(string userId);
    Task CancelAsync(string userId, bool immediately);
    Task HandleWebhookAsync(string payload, string signature);
}
