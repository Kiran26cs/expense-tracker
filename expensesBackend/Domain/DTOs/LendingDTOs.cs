namespace ExpensesBackend.API.Domain.DTOs;

public class LendingDto
{
    public string Id { get; set; } = string.Empty;
    public string ExpenseBookId { get; set; } = string.Empty;
    public string BorrowerName { get; set; } = string.Empty;
    public string? BorrowerContact { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal AnnualInterestRate { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal TotalRepaid { get; set; }
    public decimal OutstandingPrincipal { get; set; }
    public int RepaymentCount { get; set; }
    public string Status { get; set; } = "active";
    public string? Notes { get; set; }
    // Computed at read time
    public decimal AccruedInterest { get; set; }
    public decimal FutureInterest { get; set; }
    public decimal ProjectedTotalInterest { get; set; }
    public decimal TotalToRecover { get; set; }
    public bool IsOverdue { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateLendingRequest
{
    public string ExpenseBookId { get; set; } = string.Empty;
    public string BorrowerName { get; set; } = string.Empty;
    public string? BorrowerContact { get; set; }
    public decimal PrincipalAmount { get; set; }
    public decimal AnnualInterestRate { get; set; } = 0;
    public DateTime StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Notes { get; set; }
}

public class UpdateLendingRequest
{
    public string? BorrowerName { get; set; }
    public string? BorrowerContact { get; set; }
    public decimal? AnnualInterestRate { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Notes { get; set; }
}

public class RepaymentDto
{
    public string Id { get; set; } = string.Empty;
    public string LendingId { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public DateTime RecordedAt { get; set; }
}

public class CreateRepaymentRequest
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}

public class SettleLendingRequest
{
    public decimal? InterestCollected { get; set; }
    public DateTime? SettlementDate { get; set; }
    public string? Notes { get; set; }
}

public class LendingRepaymentsResponse
{
    public LendingDto Lending { get; set; } = null!;
    public List<RepaymentDto> Items { get; set; } = [];
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasMore { get; set; }
}
