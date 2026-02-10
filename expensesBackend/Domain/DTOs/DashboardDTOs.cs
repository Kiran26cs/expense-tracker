namespace ExpensesBackend.API.Domain.DTOs;

public class DashboardSummary
{
    public decimal TotalExpenses { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal Savings { get; set; }
    public List<CategoryBreakdown> CategoryBreakdown { get; set; } = new();
    public List<ExpenseDto> RecentTransactions { get; set; } = new();
}

public class CategoryBreakdown
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public double Percentage { get; set; }
}

public class MonthlyTrend
{
    public string Month { get; set; } = string.Empty;
    public decimal TotalExpenses { get; set; }
    public decimal TotalIncome { get; set; }
}

public class DailyTransactionGroup
{
    public DateTime Date { get; set; }
    public string DateLabel { get; set; } = string.Empty; // "Today", "Yesterday", or formatted date
    public List<CategorySpendingDto> CategorySpending { get; set; } = new();
    public decimal TotalSpent { get; set; }
}

public class CategorySpendingDto
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Count { get; set; }
}

public class UpcomingPaymentDto
{
    public string Id { get; set; } = string.Empty;
    public string RecurringExpenseId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Frequency { get; set; } = "monthly";
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = "upcoming";
    public string DueDateLabel { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class UpcomingPaymentsPaginatedResponse
{
    public List<UpcomingPaymentDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasMore { get; set; }
}

public class MarkUpcomingPaidRequest
{
    public DateTime PaidDate { get; set; }
    public bool RecordAsExpense { get; set; } = true;
}
