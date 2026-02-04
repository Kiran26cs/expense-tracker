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
