using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Services.Interfaces;
using System.Text;

namespace ExpensesBackend.API.Services.AI;

public class SystemPromptBuilder
{
    private readonly IExpenseService _expenseService;
    private readonly IBudgetService _budgetService;
    private readonly ICategoryService _categoryService;

    public SystemPromptBuilder(
        IExpenseService expenseService,
        IBudgetService budgetService,
        ICategoryService categoryService)
    {
        _expenseService  = expenseService;
        _budgetService   = budgetService;
        _categoryService = categoryService;
    }

    public async Task<string> BuildAsync(
        ExpenseBookResponse book,
        string userId,
        string userEmail,
        ResolvedPermissions permissions,
        ReferenceContext? reference)
    {
        var sb  = new StringBuilder();
        var now = DateTime.UtcNow;

        sb.AppendLine("You are an AI assistant embedded in an expense tracker application.");
        sb.AppendLine("You help users manage their finances conversationally — adding expenses, viewing summaries, managing budgets, and more.");
        sb.AppendLine();
        sb.AppendLine("## Active Expense Book");
        sb.AppendLine($"- Name: {book.Name}");
        sb.AppendLine($"- Currency: {book.Currency}");
        sb.AppendLine($"- Book ID: {book.Id}");
        sb.AppendLine();
        sb.AppendLine("## Current User");
        sb.AppendLine($"- Email: {userEmail}");
        sb.AppendLine($"- Role: {permissions.Role}");
        sb.AppendLine();
        sb.AppendLine("## User Permissions");
        sb.AppendLine($"- Dashboard: {permissions.Dashboard}");
        sb.AppendLine($"- Expenses: {permissions.Expenses}");
        sb.AppendLine($"- Budgets: {permissions.Budgets}");
        sb.AppendLine($"- Insights: {permissions.Insights}");
        sb.AppendLine($"- Can delete expenses: {permissions.CanDeleteExpenses}");
        sb.AppendLine($"- Can manage members: {permissions.CanManageMembers}");

        if (permissions.AllowedCategoryIds.Count > 0)
        {
            sb.AppendLine($"- Allowed category IDs (whitelist): {string.Join(", ", permissions.AllowedCategoryIds)}");
            sb.AppendLine("  IMPORTANT: Only suggest or use categories from this whitelist.");
        }

        sb.AppendLine();
        sb.AppendLine("## Date & Time");
        sb.AppendLine($"- Today: {now:yyyy-MM-dd}");
        sb.AppendLine($"- Current month: {now:MMMM yyyy}");

        sb.AppendLine();
        sb.AppendLine("## Behaviour Rules");
        sb.AppendLine("- Always use the book's currency symbol when displaying amounts.");
        sb.AppendLine("- Do not offer actions that exceed the user's permissions.");
        sb.AppendLine("- When creating or updating an expense, confirm the category if ambiguous.");
        sb.AppendLine("- When the user says 'it', 'that', or 'this', resolve against the pinned reference item below (if present).");
        sb.AppendLine("- Keep responses concise and friendly.");
        sb.AppendLine("- Do not reveal internal IDs in your reply unless the user asks.");
        sb.AppendLine("- If a tool call fails due to permissions, explain what the user cannot do and suggest what they can do instead.");

        if (reference != null)
        {
            sb.AppendLine();
            sb.AppendLine("## Pinned Reference Item");
            var referenceText = await BuildReferenceTextAsync(reference, book.Id, userId);
            sb.AppendLine(referenceText);
            sb.AppendLine("When the user refers to 'it', 'that', 'this item' etc., assume they mean this pinned item.");
        }

        return sb.ToString();
    }

    private async Task<string> BuildReferenceTextAsync(ReferenceContext reference, string bookId, string userId)
    {
        try
        {
            return reference.Type.ToLowerInvariant() switch
            {
                "expense"  => await BuildExpenseReferenceAsync(reference.Id, userId),
                "budget"   => await BuildBudgetReferenceAsync(reference.Id, bookId, userId),
                "category" => await BuildCategoryReferenceAsync(reference.Id, bookId),
                _          => $"Type: {reference.Type}, ID: {reference.Id}"
            };
        }
        catch
        {
            return $"Type: {reference.Type}, ID: {reference.Id} (details unavailable)";
        }
    }

    private async Task<string> BuildExpenseReferenceAsync(string expenseId, string userId)
    {
        var expense = await _expenseService.GetExpenseByIdAsync(userId, expenseId);
        return $"Type: Expense\n- Description: {expense.Description}\n- Amount: {expense.Amount}\n- Category: {expense.Category}\n- Date: {expense.Date:yyyy-MM-dd}\n- ID: {expense.Id}";
    }

    private async Task<string> BuildBudgetReferenceAsync(string budgetId, string bookId, string userId)
    {
        var budget = await _budgetService.GetBudgetByIdAsync(userId, budgetId);
        if (budget == null) return $"Type: Budget, ID: {budgetId}";
        return $"Type: Budget\n- Category: {budget.Category}\n- Amount: {budget.Amount}\n- Period: {budget.Period}\n- ID: {budget.Id}";
    }

    private async Task<string> BuildCategoryReferenceAsync(string categoryId, string bookId)
    {
        var category = await _categoryService.GetCategoryByIdAsync(bookId, categoryId);
        return $"Type: Category\n- Name: {category.Name}\n- ID: {category.Id}";
    }
}
