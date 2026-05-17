using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Services.Interfaces;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ExpensesBackend.API.Services.AI;

public class ToolRegistry
{
    private readonly IExpenseService _expenses;
    private readonly IBudgetService _budgets;
    private readonly ICategoryService _categories;
    private readonly IDashboardService _dashboard;
    private readonly IMemberService _members;
    private readonly IPermissionService _permissions;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition      = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented               = false,
    };

    public ToolRegistry(
        IExpenseService expenses,
        IBudgetService budgets,
        ICategoryService categories,
        IDashboardService dashboard,
        IMemberService members,
        IPermissionService permissions)
    {
        _expenses    = expenses;
        _budgets     = budgets;
        _categories  = categories;
        _dashboard   = dashboard;
        _members     = members;
        _permissions = permissions;
    }

    // ── Tool definitions exposed to Claude ────────────────────────────────────

    public IReadOnlyList<ToolDefinition> GetDefinitions(ResolvedPermissions perms)
    {
        var tools = new List<ToolDefinition>
        {
            // Categories (always available to any member)
            new("list_categories",
                "List all expense categories available in the active book.",
                """{"type":"object","properties":{},"required":[]}"""),

            // DISABLED — enable in a future phase
            // new("get_dashboard_summary", ...),
            // new("get_spending_trends", ...),
        };

        // Expenses
        // DISABLED — enable in a future phase
        // if (perms.Expenses != "none")
        // {
        //     tools.Add(new("list_expenses", ...));
        //     tools.Add(new("get_expense", ...));
        // }

        if (perms.Expenses == "write")
        {
            tools.Add(new("create_expense",
                "Create a new expense in the active book.",
                """{"type":"object","properties":{"amount":{"type":"number"},"description":{"type":"string"},"category":{"type":"string","description":"Category name or ID"},"date":{"type":"string","description":"ISO date YYYY-MM-DD (defaults to today)"},"paymentMethod":{"type":"string","description":"e.g. cash, card, UPI"},"notes":{"type":"string"}},"required":["amount","category","paymentMethod"]}"""));

            tools.Add(new("update_expense",
                "Update an existing expense. Only provide fields that should change.",
                """{"type":"object","properties":{"expenseId":{"type":"string"},"amount":{"type":"number"},"description":{"type":"string"},"category":{"type":"string"},"date":{"type":"string","description":"ISO date YYYY-MM-DD"},"paymentMethod":{"type":"string"},"notes":{"type":"string"}},"required":["expenseId"]}"""));
        }

        if (perms.CanDeleteExpenses)
        {
            tools.Add(new("delete_expense",
                "Permanently delete an expense by its ID.",
                """{"type":"object","properties":{"expenseId":{"type":"string"}},"required":["expenseId"]}"""));
        }

        // Budgets
        // DISABLED — enable in a future phase
        // if (perms.Budgets != "none")
        // {
        //     tools.Add(new("list_budgets", ...));
        // }

        if (perms.Budgets == "write")
        {
            tools.Add(new("set_budget",
                "Create or update a budget for a category in a given month.",
                """{"type":"object","properties":{"category":{"type":"string","description":"Category name or ID"},"amount":{"type":"number"},"month":{"type":"string","description":"YYYY-MM (defaults to current month)"}},"required":["category","amount"]}"""));

            tools.Add(new("delete_budget",
                "Delete a budget by its ID.",
                """{"type":"object","properties":{"budgetId":{"type":"string"}},"required":["budgetId"]}"""));
        }

        // Members
        if (perms.CanManageMembers)
        {
            // DISABLED — enable in a future phase
            // tools.Add(new("list_members", ...));

            tools.Add(new("invite_member",
                "Invite a user to the expense book by email.",
                """{"type":"object","properties":{"email":{"type":"string"},"role":{"type":"string","enum":["admin","member","viewer"]}},"required":["email","role"]}"""));

            tools.Add(new("remove_member",
                "Remove a member from the expense book by their member ID.",
                """{"type":"object","properties":{"memberId":{"type":"string"}},"required":["memberId"]}"""));
        }

        return tools;
    }

    // ── Tool executor — called by ClaudeOrchestrator ──────────────────────────

    public async Task<string> ExecuteAsync(string toolName, JsonObject args, ToolExecutionContext ctx)
    {
        return toolName switch
        {
            "list_categories"      => await ListCategoriesAsync(ctx),
            "get_dashboard_summary"=> await GetDashboardSummaryAsync(args, ctx),
            "get_spending_trends"  => await GetSpendingTrendsAsync(args, ctx),
            "list_expenses"        => await ListExpensesAsync(args, ctx),
            "get_expense"          => await GetExpenseAsync(args, ctx),
            "create_expense"       => await CreateExpenseAsync(args, ctx),
            "update_expense"       => await UpdateExpenseAsync(args, ctx),
            "delete_expense"       => await DeleteExpenseAsync(args, ctx),
            "list_budgets"         => await ListBudgetsAsync(args, ctx),
            "set_budget"           => await SetBudgetAsync(args, ctx),
            "delete_budget"        => await DeleteBudgetAsync(args, ctx),
            "list_members"         => await ListMembersAsync(ctx),
            "invite_member"        => await InviteMemberAsync(args, ctx),
            "remove_member"        => await RemoveMemberAsync(args, ctx),
            _                      => $"Unknown tool: {toolName}"
        };
    }

    // ── Category tools ────────────────────────────────────────────────────────

    private async Task<string> ListCategoriesAsync(ToolExecutionContext ctx)
    {
        var cats = await _categories.GetCategoriesAsync(ctx.BookId);
        return Serialize(cats.Select(c => new { c.Id, c.Name, c.Icon, c.Color }));
    }

    // ── Dashboard tools ───────────────────────────────────────────────────────

    private async Task<string> GetDashboardSummaryAsync(JsonObject args, ToolExecutionContext ctx)
    {
        await _permissions.AssertCanAsync(ctx.BookId, ctx.UserId, "dashboard");
        var start = ParseDate(args["startDate"]);
        var end   = ParseDate(args["endDate"]);
        var summary = await _dashboard.GetSummaryAsync(
            ctx.UserId, ctx.BookId, start, end, ctx.Permissions.AllowedCategoryIds);
        return Serialize(summary);
    }

    private async Task<string> GetSpendingTrendsAsync(JsonObject args, ToolExecutionContext ctx)
    {
        await _permissions.AssertCanAsync(ctx.BookId, ctx.UserId, "insights");
        var months = args["months"]?.GetValue<int>() ?? 6;
        var trends = await _dashboard.GetMonthlyTrendsAsync(
            ctx.UserId, ctx.BookId, months, ctx.Permissions.AllowedCategoryIds);
        return Serialize(trends);
    }

    // ── Expense tools ─────────────────────────────────────────────────────────

    private async Task<string> ListExpensesAsync(JsonObject args, ToolExecutionContext ctx)
    {
        await _permissions.AssertCanAsync(ctx.BookId, ctx.UserId, "expenses");

        var category = args["category"]?.GetValue<string>();
        if (ctx.Permissions.AllowedCategoryIds.Count > 0
            && !string.IsNullOrEmpty(category)
            && !ctx.Permissions.AllowedCategoryIds.Contains(category))
            throw new UnauthorizedAccessException("You do not have access to this category.");

        var req = new ExpensePagedRequest
        {
            ExpenseBookId      = ctx.BookId,
            Search             = args["search"]?.GetValue<string>(),
            Category           = category,
            StartDate          = ParseDate(args["startDate"]),
            EndDate            = ParseDate(args["endDate"]),
            SortField          = "date",
            SortDir            = "desc",
            PageSize           = args["pageSize"]?.GetValue<int>() ?? 20,
            AllowedCategoryIds = ctx.Permissions.AllowedCategoryIds,
        };

        var result = await _expenses.GetExpensesPagedAsync(ctx.UserId, req);
        return Serialize(result.Items?.Select(e => new
        {
            e.Id, e.Amount, e.Description, e.Category, e.Date, e.PaymentMethod
        }));
    }

    private async Task<string> GetExpenseAsync(JsonObject args, ToolExecutionContext ctx)
    {
        await _permissions.AssertCanAsync(ctx.BookId, ctx.UserId, "expenses");
        var id      = args["expenseId"]!.GetValue<string>();
        var expense = await _expenses.GetExpenseByIdAsync(ctx.UserId, id);
        return Serialize(expense);
    }

    private async Task<string> CreateExpenseAsync(JsonObject args, ToolExecutionContext ctx)
    {
        await _permissions.AssertCanAsync(ctx.BookId, ctx.UserId, "expenses", "write");

        var category = args["category"]!.GetValue<string>();
        if (ctx.Permissions.AllowedCategoryIds.Count > 0
            && !ctx.Permissions.AllowedCategoryIds.Contains(category))
            throw new UnauthorizedAccessException("You are not allowed to use this category.");

        var dateStr = args["date"]?.GetValue<string>();
        var date    = string.IsNullOrEmpty(dateStr) ? DateTime.UtcNow : DateTime.Parse(dateStr);

        var req = new CreateExpenseRequest
        {
            ExpenseBookId = ctx.BookId,
            Amount        = args["amount"]!.GetValue<decimal>(),
            Description   = args["description"]?.GetValue<string>(),
            Category      = category,
            Date          = date,
            PaymentMethod = args["paymentMethod"]!.GetValue<string>(),
            Notes         = args["notes"]?.GetValue<string>(),
        };

        var expense = await _expenses.CreateExpenseAsync(ctx.UserId, req);
        return Serialize(new { expense.Id, expense.Amount, expense.Description, expense.Category, expense.Date });
    }

    private async Task<string> UpdateExpenseAsync(JsonObject args, ToolExecutionContext ctx)
    {
        await _permissions.AssertCanAsync(ctx.BookId, ctx.UserId, "expenses", "write");

        var category = args["category"]?.GetValue<string>();
        if (category != null && ctx.Permissions.AllowedCategoryIds.Count > 0
            && !ctx.Permissions.AllowedCategoryIds.Contains(category))
            throw new UnauthorizedAccessException("You are not allowed to use this category.");

        var dateStr = args["date"]?.GetValue<string>();
        var req = new UpdateExpenseRequest
        {
            Amount        = args["amount"]?.GetValue<decimal>(),
            Description   = args["description"]?.GetValue<string>(),
            Category      = category,
            Date          = string.IsNullOrEmpty(dateStr) ? null : DateTime.Parse(dateStr),
            PaymentMethod = args["paymentMethod"]?.GetValue<string>(),
            Notes         = args["notes"]?.GetValue<string>(),
        };

        var expense = await _expenses.UpdateExpenseAsync(ctx.UserId, args["expenseId"]!.GetValue<string>(), req);
        return Serialize(new { expense.Id, expense.Amount, expense.Description, expense.Category, expense.Date });
    }

    private async Task<string> DeleteExpenseAsync(JsonObject args, ToolExecutionContext ctx)
    {
        await _permissions.AssertCanDeleteExpensesAsync(ctx.BookId, ctx.UserId);
        var deleted = await _expenses.DeleteExpenseAsync(ctx.UserId, args["expenseId"]!.GetValue<string>());
        return deleted ? "Expense deleted successfully." : "Expense not found.";
    }

    // ── Budget tools ──────────────────────────────────────────────────────────

    private async Task<string> ListBudgetsAsync(JsonObject args, ToolExecutionContext ctx)
    {
        await _permissions.AssertCanAsync(ctx.BookId, ctx.UserId, "budgets");
        var month   = args["month"]?.GetValue<string>();
        var budgets = await _budgets.GetBudgetsAsync(ctx.UserId, ctx.BookId, month);
        return Serialize(budgets.Select(b => new { b.Id, b.Category, b.Amount, b.Period }));
    }

    private async Task<string> SetBudgetAsync(JsonObject args, ToolExecutionContext ctx)
    {
        await _permissions.AssertCanAsync(ctx.BookId, ctx.UserId, "budgets", "write");

        var monthStr = args["month"]?.GetValue<string>()
            ?? DateTime.UtcNow.ToString("yyyy-MM");

        var effectiveDate = DateTime.Parse($"{monthStr}-01");

        var categoryName = await ResolveCategoryNameAsync(args["category"]!.GetValue<string>(), ctx.BookId);

        var budget = await _budgets.UpsertBudgetVersionAsync(
            ctx.UserId,
            ctx.BookId,
            categoryName,
            args["amount"]!.GetValue<decimal>(),
            effectiveDate,
            monthStr);

        return Serialize(new { budget.Id, budget.Category, budget.Amount, budget.Period });
    }

    /// <summary>
    /// Accepts either a category ID or name and always returns the canonical name.
    /// Falls back to the raw value if no match is found.
    /// </summary>
    private async Task<string> ResolveCategoryNameAsync(string idOrName, string bookId)
    {
        var cats = await _categories.GetCategoriesAsync(bookId);
        var match = cats.FirstOrDefault(c =>
            c.Id.Equals(idOrName, StringComparison.OrdinalIgnoreCase) ||
            c.Name.Equals(idOrName, StringComparison.OrdinalIgnoreCase));
        return match?.Name ?? idOrName;
    }

    private async Task<string> DeleteBudgetAsync(JsonObject args, ToolExecutionContext ctx)
    {
        await _permissions.AssertCanAsync(ctx.BookId, ctx.UserId, "budgets", "write");
        var deleted = await _budgets.DeleteBudgetAsync(ctx.UserId, args["budgetId"]!.GetValue<string>());
        return deleted ? "Budget deleted." : "Budget not found.";
    }

    // ── Member tools ──────────────────────────────────────────────────────────

    private async Task<string> ListMembersAsync(ToolExecutionContext ctx)
    {
        await _permissions.AssertCanManageMembersAsync(ctx.BookId, ctx.UserId);
        var members = await _members.GetMembersAsync(ctx.BookId, ctx.UserId);
        return Serialize(members.Select(m => new { m.Id, m.InvitedEmail, m.Role, m.InviteStatus }));
    }

    private async Task<string> InviteMemberAsync(JsonObject args, ToolExecutionContext ctx)
    {
        await _permissions.AssertCanManageMembersAsync(ctx.BookId, ctx.UserId);
        var req = new InviteMemberRequest
        {
            Email = args["email"]!.GetValue<string>(),
            Role  = args["role"]!.GetValue<string>(),
        };
        var result = await _members.InviteMemberAsync(ctx.BookId, ctx.UserId, req, string.Empty);
        return $"Invitation sent to {req.Email} as {req.Role}.";
    }

    private async Task<string> RemoveMemberAsync(JsonObject args, ToolExecutionContext ctx)
    {
        await _permissions.AssertCanManageMembersAsync(ctx.BookId, ctx.UserId);
        await _members.RemoveMemberAsync(ctx.BookId, args["memberId"]!.GetValue<string>(), ctx.UserId);
        return "Member removed successfully.";
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string Serialize(object? obj)
        => JsonSerializer.Serialize(obj, JsonOpts);

    private static DateTime? ParseDate(JsonNode? node)
    {
        var str = node?.GetValue<string>();
        return string.IsNullOrEmpty(str) ? null : DateTime.Parse(str);
    }
}
