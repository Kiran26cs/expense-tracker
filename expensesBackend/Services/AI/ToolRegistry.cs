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
    private readonly ILendingService _lendings;

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
        IPermissionService permissions,
        ILendingService lendings)
    {
        _expenses    = expenses;
        _budgets     = budgets;
        _categories  = categories;
        _dashboard   = dashboard;
        _members     = members;
        _permissions = permissions;
        _lendings    = lendings;
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
                "Create a new expense in the active book. If the user mentions a foreign currency amount, supply originalAmount and originalCurrency; the system will convert to the book currency automatically.",
                """{"type":"object","properties":{"amount":{"type":"number","description":"Amount in book currency. If originalCurrency is supplied this can be omitted — it will be calculated automatically."},"description":{"type":"string"},"category":{"type":"string","description":"Category name or ID"},"date":{"type":"string","description":"ISO date YYYY-MM-DD (defaults to today)"},"paymentMethod":{"type":"string","description":"e.g. cash, card, UPI"},"notes":{"type":"string"},"originalAmount":{"type":"number","description":"Amount in the original foreign currency (omit if same as book currency)"},"originalCurrency":{"type":"string","description":"ISO 4217 code of the foreign currency, e.g. USD, EUR (omit if same as book currency)"}},"required":["category","paymentMethod"]}"""));

            tools.Add(new("update_expense",
                "Update an existing expense. Only provide fields that should change.",
                """{"type":"object","properties":{"expenseId":{"type":"string"},"amount":{"type":"number"},"description":{"type":"string"},"category":{"type":"string"},"date":{"type":"string","description":"ISO date YYYY-MM-DD"},"paymentMethod":{"type":"string"},"notes":{"type":"string"}},"required":["expenseId"]}"""));

            tools.Add(new("create_recurring_expense",
                "Set up a recurring expense (e.g. monthly loan EMI, rent, subscription). Creates a scheduled entry tracked in Finance Tools. The expense is recorded each time the user marks it as paid.",
                """{"type":"object","properties":{"amount":{"type":"number"},"category":{"type":"string","description":"Category name or ID"},"description":{"type":"string","description":"Label for the recurring expense, e.g. 'Home Loan EMI'"},"paymentMethod":{"type":"string","description":"e.g. cash, card, UPI, bank transfer"},"frequency":{"type":"string","enum":["daily","weekly","monthly","yearly"],"description":"How often it recurs (default: monthly)"},"startDate":{"type":"string","description":"ISO date YYYY-MM-DD — date of first occurrence"},"endDate":{"type":"string","description":"ISO date YYYY-MM-DD — optional end date"},"notes":{"type":"string"}},"required":["amount","category","paymentMethod","startDate"]}"""));

            tools.Add(new("create_expense_batch",
                "Save multiple line items from a receipt as individual expense entries. Each item gets its own expense document. If taxAmount is provided, tax is stored as a separate entry with category 'Tax & Fees'. All items share the same receiptGroupId. Use this after reading a receipt — first call list_categories to map categories correctly.",
                """{"type":"object","properties":{"receiptNumber":{"type":"string","description":"Invoice or receipt number (optional)"},"merchant":{"type":"string","description":"Merchant or store name (optional)"},"paymentMethod":{"type":"string","description":"e.g. cash, card, UPI"},"date":{"type":"string","description":"ISO date YYYY-MM-DD"},"items":{"type":"array","description":"Line items from the receipt","items":{"type":"object","properties":{"name":{"type":"string","description":"Item name or description"},"amount":{"type":"number","description":"Item amount"},"category":{"type":"string","description":"Existing category name or ID"}},"required":["amount","category"]}},"taxAmount":{"type":"number","description":"Tax amount (optional) — stored as a separate expense entry with category Tax & Fees"},"taxLabel":{"type":"string","description":"Tax label from receipt, e.g. GST, VAT (optional)"}},"required":["paymentMethod","date","items"]}"""));

            tools.Add(new("create_lending",
                "Record money lent to someone. Tracks the principal, optional interest rate, repayments, and due date. Use this when the user says they lent money to a person.",
                """{"type":"object","properties":{"borrowerName":{"type":"string","description":"Name of the person who borrowed the money"},"borrowerContact":{"type":"string","description":"Optional phone or email of the borrower"},"principalAmount":{"type":"number","description":"Amount lent"},"annualInterestRate":{"type":"number","description":"Annual interest rate in percent, e.g. 12 for 12%. Default 0 for interest-free."},"startDate":{"type":"string","description":"ISO date YYYY-MM-DD — date money was lent (defaults to today)"},"dueDate":{"type":"string","description":"ISO date YYYY-MM-DD — optional repayment deadline"},"notes":{"type":"string"}},"required":["borrowerName","principalAmount"]}"""));
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

        // Categories — owner-only (mirrors VerifyBookOwnershipAsync in SettingsController)
        if (perms.IsOwner)
        {
            tools.Add(new("create_category",
                "Create a new custom expense category in this book.",
                """{"type":"object","properties":{"name":{"type":"string","description":"Category name, e.g. 'Groceries', 'Loan'"},"type":{"type":"string","enum":["expense","income"],"description":"Whether this category is for expenses or income (default: expense)"},"icon":{"type":"string","description":"Font Awesome icon class, e.g. 'fa-solid fa-tag' (default if omitted)"},"color":{"type":"string","description":"Hex color code, e.g. '#6366f1' (default if omitted)"},"financialClass":{"type":"string","enum":["need","want","debt"],"description":"Financial classification for the 50-30-20 rule: 'need' (essential expenses like rent/groceries), 'want' (discretionary like dining/entertainment), 'debt' (loan repayments/EMIs). Omit to use rule-based auto-detection."}},"required":["name"]}"""));

            tools.Add(new("update_category",
                "Rename, change the icon/color, or set the financial classification of an existing custom category. Cannot modify default categories.",
                """{"type":"object","properties":{"category":{"type":"string","description":"Category name or ID to update"},"name":{"type":"string","description":"New name for the category"},"type":{"type":"string","enum":["expense","income"]},"icon":{"type":"string","description":"New Font Awesome icon class"},"color":{"type":"string","description":"New hex color code"},"financialClass":{"type":"string","enum":["need","want","debt"],"description":"Set financial classification: 'need', 'want', or 'debt'"},"clearFinancialClass":{"type":"boolean","description":"Set to true to remove the manual classification and revert to auto-detection"}},"required":["category"]}"""));

            tools.Add(new("delete_category",
                "Delete a custom category from this book. Default categories cannot be deleted.",
                """{"type":"object","properties":{"category":{"type":"string","description":"Category name or ID to delete"}},"required":["category"]}"""));
        }

        return tools;
    }

    // ── Tool executor — called by ClaudeOrchestrator ──────────────────────────

    public async Task<string> ExecuteAsync(string toolName, JsonObject args, ToolExecutionContext ctx)
    {
        return toolName switch
        {
            "list_categories"          => await ListCategoriesAsync(ctx),
            "get_dashboard_summary"    => await GetDashboardSummaryAsync(args, ctx),
            "get_spending_trends"      => await GetSpendingTrendsAsync(args, ctx),
            "list_expenses"            => await ListExpensesAsync(args, ctx),
            "get_expense"              => await GetExpenseAsync(args, ctx),
            "create_expense"           => await CreateExpenseAsync(args, ctx),
            "create_expense_batch"     => await CreateExpenseBatchAsync(args, ctx),
            "update_expense"           => await UpdateExpenseAsync(args, ctx),
            "delete_expense"           => await DeleteExpenseAsync(args, ctx),
            "create_recurring_expense" => await CreateRecurringExpenseAsync(args, ctx),
            "list_budgets"             => await ListBudgetsAsync(args, ctx),
            "set_budget"               => await SetBudgetAsync(args, ctx),
            "delete_budget"            => await DeleteBudgetAsync(args, ctx),
            "list_members"             => await ListMembersAsync(ctx),
            "invite_member"            => await InviteMemberAsync(args, ctx),
            "remove_member"            => await RemoveMemberAsync(args, ctx),
            "create_lending"           => await CreateLendingAsync(args, ctx),
            "create_category"          => await CreateCategoryAsync(args, ctx),
            "update_category"          => await UpdateCategoryAsync(args, ctx),
            "delete_category"          => await DeleteCategoryAsync(args, ctx),
            _                          => $"Unknown tool: {toolName}"
        };
    }

    // ── Category tools ────────────────────────────────────────────────────────

    private async Task<string> ListCategoriesAsync(ToolExecutionContext ctx)
    {
        var cats = await _categories.GetCategoriesAsync(ctx.BookId);
        return Serialize(cats.Select(c => new { c.Id, c.Name, c.Icon, c.Color, c.FinancialClass }));
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
        if (!ctx.Permissions.IsOwner
            && ctx.Permissions.AllowedCategoryIds.Count > 0
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

        var categoryArg = args["category"]!.GetValue<string>();
        var (categoryName, categoryId) = await ResolveCategoryAsync(categoryArg, ctx.BookId);

        if (!ctx.Permissions.IsOwner
            && ctx.Permissions.AllowedCategoryIds.Count > 0
            && !ctx.Permissions.AllowedCategoryIds.Contains(categoryId))
            throw new UnauthorizedAccessException($"You are not allowed to use the category '{categoryName}'.");

        var dateStr = args["date"]?.GetValue<string>();
        var date    = string.IsNullOrEmpty(dateStr) ? DateTime.UtcNow : DateTime.Parse(dateStr);

        var originalCurrency = args["originalCurrency"]?.GetValue<string>();
        var originalAmount   = args["originalAmount"]?.GetValue<decimal?>();
        // When a foreign currency is provided, amount may be omitted — service will compute it
        var amountNode = args["amount"];
        var amount = amountNode != null ? amountNode.GetValue<decimal>()
                   : (originalAmount ?? 0m);

        var req = new CreateExpenseRequest
        {
            ExpenseBookId    = ctx.BookId,
            Amount           = amount,
            Description      = args["description"]?.GetValue<string>(),
            Category         = categoryName,
            Date             = date,
            PaymentMethod    = args["paymentMethod"]!.GetValue<string>(),
            Notes            = args["notes"]?.GetValue<string>(),
            OriginalAmount   = originalAmount,
            OriginalCurrency = originalCurrency,
        };

        var expense = await _expenses.CreateExpenseAsync(ctx.UserId, req);
        var result  = new
        {
            expense.Id, expense.Amount, expense.Description, expense.Category, expense.Date,
            expense.OriginalAmount, expense.OriginalCurrency, expense.FxRate
        };
        return Serialize(result);
    }

    private async Task<string> CreateExpenseBatchAsync(JsonObject args, ToolExecutionContext ctx)
    {
        await _permissions.AssertCanAsync(ctx.BookId, ctx.UserId, "expenses", "write");

        var dateStr = args["date"]?.GetValue<string>();
        var date    = string.IsNullOrEmpty(dateStr) ? DateTime.UtcNow : DateTime.Parse(dateStr);

        var itemsNode = args["items"]?.AsArray() ?? [];
        var items = new List<ReceiptItemRequest>();
        foreach (var itemNode in itemsNode)
        {
            if (itemNode is not JsonObject itemObj) continue;
            var categoryArg = itemObj["category"]?.GetValue<string>() ?? string.Empty;
            var (categoryName, categoryId) = await ResolveCategoryAsync(categoryArg, ctx.BookId);

            if (!ctx.Permissions.IsOwner
                && ctx.Permissions.AllowedCategoryIds.Count > 0
                && !ctx.Permissions.AllowedCategoryIds.Contains(categoryId))
                throw new UnauthorizedAccessException($"You are not allowed to use the category '{categoryName}'.");

            items.Add(new ReceiptItemRequest
            {
                Name     = itemObj["name"]?.GetValue<string>() ?? string.Empty,
                Amount   = itemObj["amount"]?.GetValue<decimal>() ?? 0m,
                Category = categoryName,
            });
        }

        var req = new CreateExpenseBatchRequest
        {
            ExpenseBookId = ctx.BookId,
            ReceiptNumber = args["receiptNumber"]?.GetValue<string>(),
            Merchant      = args["merchant"]?.GetValue<string>(),
            PaymentMethod = args["paymentMethod"]!.GetValue<string>(),
            Date          = date,
            Items         = items,
            TaxAmount     = args["taxAmount"]?.GetValue<decimal?>(),
            TaxLabel      = args["taxLabel"]?.GetValue<string>(),
        };

        var results = await _expenses.CreateExpenseBatchAsync(ctx.UserId, req);
        return Serialize(new
        {
            Count   = results.Count,
            Total   = results.Sum(e => e.Amount),
            Items   = results.Select(e => new { e.Id, e.Description, e.Amount, e.Category }),
            Message = $"{results.Count} expense{(results.Count == 1 ? "" : "s")} saved from receipt.",
        });
    }

    private async Task<string> UpdateExpenseAsync(JsonObject args, ToolExecutionContext ctx)
    {
        await _permissions.AssertCanAsync(ctx.BookId, ctx.UserId, "expenses", "write");

        string? categoryName = null;
        if (args["category"] is not null)
        {
            var (name, id) = await ResolveCategoryAsync(args["category"]!.GetValue<string>(), ctx.BookId);
            if (!ctx.Permissions.IsOwner
                && ctx.Permissions.AllowedCategoryIds.Count > 0
                && !ctx.Permissions.AllowedCategoryIds.Contains(id))
                throw new UnauthorizedAccessException($"You are not allowed to use the category '{name}'.");
            categoryName = name;
        }

        var dateStr = args["date"]?.GetValue<string>();
        var req = new UpdateExpenseRequest
        {
            Amount        = args["amount"]?.GetValue<decimal>(),
            Description   = args["description"]?.GetValue<string>(),
            Category      = categoryName,
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

    private async Task<string> CreateRecurringExpenseAsync(JsonObject args, ToolExecutionContext ctx)
    {
        await _permissions.AssertCanAsync(ctx.BookId, ctx.UserId, "expenses", "write");

        var categoryArg = args["category"]!.GetValue<string>();
        var (categoryName, categoryId) = await ResolveCategoryAsync(categoryArg, ctx.BookId);

        if (!ctx.Permissions.IsOwner
            && ctx.Permissions.AllowedCategoryIds.Count > 0
            && !ctx.Permissions.AllowedCategoryIds.Contains(categoryId))
            throw new UnauthorizedAccessException($"You are not allowed to use the category '{categoryName}'.");

        var startDateStr = args["startDate"]!.GetValue<string>();
        var startDate    = DateTime.Parse(startDateStr);
        var endDateStr   = args["endDate"]?.GetValue<string>();
        var frequency    = args["frequency"]?.GetValue<string>() ?? "monthly";

        var req = new CreateExpenseRequest
        {
            ExpenseBookId   = ctx.BookId,
            Amount          = args["amount"]!.GetValue<decimal>(),
            Category        = categoryName,
            PaymentMethod   = args["paymentMethod"]!.GetValue<string>(),
            Description     = args["description"]?.GetValue<string>(),
            Notes           = args["notes"]?.GetValue<string>(),
            Date            = startDate,
            IsRecurring     = true,
            RecurringConfig = new RecurringConfig
            {
                Frequency = frequency,
                StartDate = startDate,
                EndDate   = string.IsNullOrEmpty(endDateStr) ? null : DateTime.Parse(endDateStr),
            },
        };

        var result = await _expenses.CreateExpenseAsync(ctx.UserId, req);
        return Serialize(new
        {
            result.Id,
            result.Amount,
            result.Category,
            result.Description,
            Frequency   = frequency,
            StartDate   = startDate.ToString("yyyy-MM-dd"),
            EndDate     = string.IsNullOrEmpty(endDateStr) ? null : endDateStr,
            result.IsRecurring,
            Message = $"Recurring expense '{result.Description ?? categoryName}' set up successfully. It will appear in Finance Tools and you can mark it as paid each {frequency}.",
        });
    }

    private async Task<string> CreateLendingAsync(JsonObject args, ToolExecutionContext ctx)
    {
        await _permissions.AssertCanAsync(ctx.BookId, ctx.UserId, "expenses", "write");

        var startDateStr = args["startDate"]?.GetValue<string>();
        var dueDateStr   = args["dueDate"]?.GetValue<string>();

        var req = new CreateLendingRequest
        {
            ExpenseBookId      = ctx.BookId,
            BorrowerName       = args["borrowerName"]!.GetValue<string>(),
            BorrowerContact    = args["borrowerContact"]?.GetValue<string>(),
            PrincipalAmount    = args["principalAmount"]!.GetValue<decimal>(),
            AnnualInterestRate = args["annualInterestRate"]?.GetValue<decimal>() ?? 0,
            StartDate          = string.IsNullOrEmpty(startDateStr) ? DateTime.UtcNow : DateTime.Parse(startDateStr),
            DueDate            = string.IsNullOrEmpty(dueDateStr) ? null : DateTime.Parse(dueDateStr),
            Notes              = args["notes"]?.GetValue<string>(),
        };

        var lending = await _lendings.CreateLendingAsync(ctx.UserId, req);
        return Serialize(new
        {
            lending.Id,
            lending.BorrowerName,
            lending.PrincipalAmount,
            lending.AnnualInterestRate,
            StartDate = lending.StartDate.ToString("yyyy-MM-dd"),
            DueDate   = lending.DueDate?.ToString("yyyy-MM-dd"),
            lending.Status,
            lending.OutstandingPrincipal,
            Message = $"Lending of {lending.PrincipalAmount} to {lending.BorrowerName} recorded. You can track repayments in Finance Tools.",
        });
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
    /// Accepts either a category ID or name and returns (canonicalName, id).
    /// Falls back to (rawValue, rawValue) if no match — keeps backward compat for unrecognised values.
    /// </summary>
    private async Task<(string Name, string Id)> ResolveCategoryAsync(string idOrName, string bookId)
    {
        var cats = await _categories.GetCategoriesAsync(bookId);
        var match = cats.FirstOrDefault(c =>
            c.Id.Equals(idOrName, StringComparison.OrdinalIgnoreCase) ||
            c.Name.Equals(idOrName, StringComparison.OrdinalIgnoreCase));
        return match is not null ? (match.Name, match.Id) : (idOrName, idOrName);
    }

    private async Task<string> ResolveCategoryNameAsync(string idOrName, string bookId)
    {
        var (name, _) = await ResolveCategoryAsync(idOrName, bookId);
        return name;
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

    // ── Category tools ────────────────────────────────────────────────────────

    private async Task<string> CreateCategoryAsync(JsonObject args, ToolExecutionContext ctx)
    {
        await _permissions.AssertIsOwnerAsync(ctx.BookId, ctx.UserId);

        var req = new CreateCategoryRequest
        {
            ExpenseBookId  = ctx.BookId,
            Name           = args["name"]!.GetValue<string>(),
            Type           = args["type"]?.GetValue<string>()  ?? "expense",
            Icon           = args["icon"]?.GetValue<string>()  ?? "fa-solid fa-tag",
            Color          = args["color"]?.GetValue<string>() ?? "#6366f1",
            FinancialClass = args["financialClass"]?.GetValue<string>(),
        };

        var category = await _categories.CreateCategoryAsync(ctx.BookId, ctx.UserId, req);
        return Serialize(new
        {
            category.Id,
            category.Name,
            category.Type,
            category.Icon,
            category.Color,
            category.FinancialClass,
            Message = $"Category '{category.Name}' created successfully.",
        });
    }

    private async Task<string> UpdateCategoryAsync(JsonObject args, ToolExecutionContext ctx)
    {
        await _permissions.AssertIsOwnerAsync(ctx.BookId, ctx.UserId);

        var (_, categoryId) = await ResolveCategoryAsync(args["category"]!.GetValue<string>(), ctx.BookId);

        var req = new UpdateCategoryRequest
        {
            Name                = args["name"]?.GetValue<string>(),
            Type                = args["type"]?.GetValue<string>(),
            Icon                = args["icon"]?.GetValue<string>(),
            Color               = args["color"]?.GetValue<string>(),
            FinancialClass      = args["financialClass"]?.GetValue<string>(),
            ClearFinancialClass = args["clearFinancialClass"]?.GetValue<bool>() ?? false,
        };

        var category = await _categories.UpdateCategoryAsync(ctx.BookId, categoryId, req);
        return Serialize(new
        {
            category.Id,
            category.Name,
            category.Type,
            category.Icon,
            category.Color,
            category.FinancialClass,
            Message = $"Category '{category.Name}' updated successfully.",
        });
    }

    private async Task<string> DeleteCategoryAsync(JsonObject args, ToolExecutionContext ctx)
    {
        await _permissions.AssertIsOwnerAsync(ctx.BookId, ctx.UserId);

        var (categoryName, categoryId) = await ResolveCategoryAsync(args["category"]!.GetValue<string>(), ctx.BookId);

        await _categories.DeleteCategoryAsync(ctx.BookId, categoryId, ctx.UserId);
        return $"Category '{categoryName}' deleted successfully.";
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
