namespace ExpensesBackend.API.Domain.DTOs;

public record CreateSubscriptionRequestDto(string Plan);

public record CreateSubscriptionResponseDto(
    string SubscriptionId,
    string RazorpayKeyId,
    string PlanName,
    decimal Amount,
    string Currency,
    string Description
);

public record VerifyPaymentRequestDto(
    string RazorpayPaymentId,
    string RazorpaySubscriptionId,
    string RazorpaySignature
);

public record SubscriptionStatusDto(
    string Plan,
    string Status,
    DateTime? CurrentPeriodEnd,
    bool CancelAtPeriodEnd
);

public record CancelSubscriptionRequestDto(bool Immediately = false);
