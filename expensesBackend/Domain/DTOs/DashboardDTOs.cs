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
