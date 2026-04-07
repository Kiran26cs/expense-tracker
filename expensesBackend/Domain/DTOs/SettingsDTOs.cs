namespace ExpensesBackend.API.Domain.DTOs;

public enum PaymentMethodType
{
    Cash = 0,
    CreditCard = 1,
    DebitCard = 2,
    BankTransfer = 3,
    UPI = 4,
    Cheque = 5,
    Other = 6
}

public class PaymentMethodDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class UserSettingsDto
{
    public string DefaultCurrency { get; set; } = "USD";
    public decimal MonthlySavingsGoal { get; set; }
}

public class UpdateUserSettingsRequest
{
    public string? DefaultCurrency { get; set; }
    public decimal? MonthlySavingsGoal { get; set; }
}
