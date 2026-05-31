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
        sb.AppendLine("- When the user says 'it', 'that', or 'this', resolve against the pinned reference item below (if present).");
        sb.AppendLine("- Keep responses concise and friendly.");
        sb.AppendLine("- Do not reveal internal IDs in your reply unless the user asks.");
        sb.AppendLine("- If a tool call fails due to permissions, explain what the user cannot do and suggest what they can do instead.");
        sb.AppendLine();
        sb.AppendLine("## Currency Conversion");
        sb.AppendLine("- If the user mentions a foreign currency amount (e.g. '$50', '50 USD', '€30'), set originalAmount and originalCurrency in create_expense.");
        sb.AppendLine("- The system will automatically look up the live exchange rate and convert to the book's currency.");
        sb.AppendLine("- Confirm with the user by stating both values, e.g. '$50 (≈ ₹4,150)'.");
        sb.AppendLine("- If conversion fails (unsupported currency pair), tell the user: 'I couldn't look up a rate for [currency] — please tell me the amount in [bookCurrency] instead.'");
        sb.AppendLine();
        sb.AppendLine("## Information Extraction Rules (CRITICAL)");
        sb.AppendLine("- ALWAYS scan the FULL conversation history for information before asking any questions.");
        sb.AppendLine("- If the user provided a detail in a prior turn (amount, category, date, payment method, etc.) treat it as already known — do NOT ask for it again.");
        sb.AppendLine("- When the user provides multiple records in one message (e.g. 'car loan 19400 and land loan 27000'), create ALL of them in parallel using multiple tool calls. Do not ask for confirmation — proceed immediately.");
        sb.AppendLine("- Only ask for a missing field if it is genuinely absent from the entire conversation. Ask for ALL missing fields in one message, never one at a time.");
        sb.AppendLine("- Reasonable defaults you should apply silently (no need to ask): paymentMethod=Cash, frequency=monthly, startDate=today, interestRate=0, endDate=none.");
        sb.AppendLine("- Category matching: if the user says 'loan', match it to the 'loan' category. Do not ask to confirm obvious matches.");
        sb.AppendLine();
        sb.AppendLine("## Financial Classification (50-30-20 Rule)");
        sb.AppendLine("- Categories can be tagged with a financial class: 'need' (essentials), 'want' (discretionary), or 'debt' (loan/EMI obligations).");
        sb.AppendLine("- The dashboard uses these classes to show spending by bucket and measure against the 50-30-20 targets.");
        sb.AppendLine("- When the user asks to classify, tag, or set the financial class of a category, use update_category with financialClass.");
        sb.AppendLine("- When the user asks to remove or clear a category's classification, use update_category with clearFinancialClass: true.");
        sb.AppendLine("- When creating a category where the financial class is obvious from context (e.g. 'Home Loan' → 'debt'), set financialClass automatically.");
        sb.AppendLine();
        sb.AppendLine("## Receipt Itemization Rules");
        sb.AppendLine("- When asked to save a receipt with multiple items, call list_categories FIRST to get existing categories.");
        sb.AppendLine("- Map each item's suggestedCategory to the closest existing category by meaning. Never create a duplicate category.");
        sb.AppendLine("- Only call create_category if no existing category is a reasonable semantic match for an item.");
        sb.AppendLine("- Then call create_expense_batch with all items in a single tool call.");
        sb.AppendLine("- Tax (if present) is passed as taxAmount — it becomes its own expense entry automatically; do not add it as an item.");
        sb.AppendLine("- When the user chooses single-entry mode, call create_expense once with the total amount and merchant as description.");
        sb.AppendLine("- After saving, summarise: N items saved, total amount, receipt number (if present).");

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
        var cls = category.FinancialClass ?? "(auto-detect)";
        return $"Type: Category\n- Name: {category.Name}\n- FinancialClass: {cls}\n- ID: {category.Id}";
    }
}
