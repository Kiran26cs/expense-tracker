using ExpensesBackend.API.Domain;
using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Domain.Entities;
using ExpensesBackend.API.Infrastructure.Cache;
using ExpensesBackend.API.Infrastructure.Data;
using ExpensesBackend.API.Services.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text;
using System.Text.Json;

namespace ExpensesBackend.API.Services;

public class ExpenseService : IExpenseService
{
    private readonly MongoDbContext _context;
    private readonly ICacheService _cache;
    private readonly ICurrencyConversionService _fx;

    public ExpenseService(MongoDbContext context, ICacheService cache, ICurrencyConversionService fx)
    {
        _context = context;
        _cache   = cache;
        _fx      = fx;
    }

    public async Task<List<ExpenseDto>> GetExpensesAsync(string userId, string? expenseBookId, DateTime? startDate, DateTime? endDate, string? category)
    {
        var filterBuilder = Builders<Expense>.Filter;
        var filter = !string.IsNullOrEmpty(expenseBookId)
            ? filterBuilder.Eq(e => e.ExpenseBookId, expenseBookId)
            : filterBuilder.Eq(e => e.UserId, userId);

        if (startDate.HasValue)
            filter &= filterBuilder.Gte(e => e.Date, startDate.Value);

        if (endDate.HasValue)
            filter &= filterBuilder.Lte(e => e.Date, endDate.Value);

        if (!string.IsNullOrEmpty(category))
            filter &= filterBuilder.Eq(e => e.Category, category);

        var expenses = await _context.Expenses
            .Find(filter)
            .SortByDescending(e => e.Date)
            .ToListAsync();

        return expenses.Select(MapToExpenseDto).ToList();
    }

    public async Task<ExpensePagedResponse> GetExpensesPagedAsync(string userId, ExpensePagedRequest req)
    {
            var sortField = req.SortField?.ToLowerInvariant() == "amount" ? "amount" : "date";
        var sortDesc  = req.SortDir?.ToLowerInvariant() != "asc";

        // ── 1. Build base filter (same filter applied to both data + count) ──────
        var fb = Builders<BsonDocument>.Filter;
        var filter = !string.IsNullOrEmpty(req.ExpenseBookId)
            ? fb.Eq("expenseBookId", req.ExpenseBookId)
            : fb.Eq("userId", userId);
        if (!string.IsNullOrEmpty(req.Type))
            filter &= fb.Eq("type", req.Type);
        if (!string.IsNullOrEmpty(req.Category))
            filter &= fb.Eq("category", req.Category);
        if (!string.IsNullOrEmpty(req.PaymentMethod))
            filter &= fb.Eq("paymentMethod", req.PaymentMethod);
        if (req.StartDate.HasValue)
            filter &= fb.Gte("date", req.StartDate.Value);
        if (req.EndDate.HasValue)
            filter &= fb.Lte("date", req.EndDate.Value);
        if (!string.IsNullOrEmpty(req.Search))
        {
            var searchRegex = new BsonRegularExpression(req.Search, "i");
            filter &= fb.Or(
                fb.Regex("description", searchRegex),
                fb.Regex("category", searchRegex),
                fb.Regex("notes", searchRegex)
            );
        }

        // ACL: restrict to allowed categories when the member has category restrictions
        if (req.AllowedCategoryIds.Count > 0)
            filter &= fb.In("category", req.AllowedCategoryIds);

        // ── 2. Keyset cursor predicate ────────────────────────────────────────
        FilterDefinition<BsonDocument> dataFilter = filter;
        bool hasPrev = false;

        if (!string.IsNullOrEmpty(req.Cursor))
        {
            try
            {
                var cursorJson = Encoding.UTF8.GetString(Convert.FromBase64String(req.Cursor));
                var cursor = JsonSerializer.Deserialize<ExpenseCursor>(cursorJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (cursor != null)
                {
                    hasPrev = true; // if we have a cursor we came from somewhere

                    if (sortField == "date")
                    {
                        var cursorDate = DateTime.Parse(cursor.Value, null,
                            System.Globalization.DateTimeStyles.RoundtripKind);
                        // For descending: next page means date < cursorDate, OR same date AND _id < cursorId
                        // For ascending:  next page means date > cursorDate, OR same date AND _id > cursorId
                        dataFilter = sortDesc
                            ? filter & fb.Or(
                                fb.Lt("date", cursorDate),
                                fb.And(fb.Eq("date", cursorDate), fb.Lt("_id", cursor.Id)))
                            : filter & fb.Or(
                                fb.Gt("date", cursorDate),
                                fb.And(fb.Eq("date", cursorDate), fb.Gt("_id", cursor.Id)));
                    }
                    else // amount
                    {
                        var cursorAmt = decimal.Parse(cursor.Value,
                            System.Globalization.CultureInfo.InvariantCulture);
                        dataFilter = sortDesc
                            ? filter & fb.Or(
                                fb.Lt("amount", cursorAmt),
                                fb.And(fb.Eq("amount", (BsonDecimal128)cursorAmt), fb.Lt("_id", cursor.Id)))
                            : filter & fb.Or(
                                fb.Gt("amount", cursorAmt),
                                fb.And(fb.Eq("amount", (BsonDecimal128)cursorAmt), fb.Gt("_id", cursor.Id)));
                    }
                }
            }
            catch { /* malformed cursor → ignore, return first page */ }
        }

        // ── 3. Build sort ────────────────────────────────────────────────────
        var sortDef = sortDesc
            ? Builders<BsonDocument>.Sort.Descending(sortField).Descending("_id")
            : Builders<BsonDocument>.Sort.Ascending(sortField).Ascending("_id");

        var collection = _context.Expenses.Database
            .GetCollection<BsonDocument>("expenses");

        // ── 4. Run count (base filter, no cursor) + data in parallel ─────────
        var countTask = collection.CountDocumentsAsync(filter);
        var docsTask  = collection
            .Find(dataFilter)
            .Sort(sortDef)
            .Limit(req.PageSize + 1)   // fetch one extra to determine hasNext
            .ToListAsync();

        await Task.WhenAll(countTask, docsTask);

        var total = countTask.Result;
        var docs  = docsTask.Result;

        var hasNext = docs.Count > req.PageSize;
        if (hasNext) docs.RemoveAt(docs.Count - 1);

        // ── 5. Map to DTOs ────────────────────────────────────────────────────
        var items = docs.Select(MapBsonToDto).ToList();

        // ── 6. Build next cursor from last item ───────────────────────────────
        string? nextCursor = null;
        string? prevCursor = null;

        if (hasNext && items.Count > 0)
        {
            var last = items[^1];
            var nc = new ExpenseCursor
            {
                Field = sortField,
                Dir   = sortDesc ? "desc" : "asc",
                Value = sortField == "date"
                    ? last.Date.ToString("o")
                    : last.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Id = last.Id
            };
            nextCursor = Convert.ToBase64String(
                Encoding.UTF8.GetBytes(JsonSerializer.Serialize(nc)));
        }

        // prevCursor points back to the item just before first item of this page
        if (hasPrev && items.Count > 0)
        {
            var first = items[0];
            var pc = new ExpenseCursor
            {
                Field = sortField,
                Dir   = sortDesc ? "asc" : "desc",  // reversed to go back
                Value = sortField == "date"
                    ? first.Date.ToString("o")
                    : first.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Id = first.Id
            };
            prevCursor = Convert.ToBase64String(
                Encoding.UTF8.GetBytes(JsonSerializer.Serialize(pc)));
        }

        return new ExpensePagedResponse
        {
            Items     = items,
            Total     = total,
            NextCursor = nextCursor,
            PrevCursor = prevCursor,
            HasNext   = hasNext,
            HasPrev   = hasPrev
        };
        }

    private static ExpenseDto MapBsonToDto(BsonDocument d) => new()
    {
        Id               = d["_id"].AsString,
        ExpenseBookId    = d.TryGetValue("expenseBookId", out var bid) ? bid.AsString : null,
        Type             = d.TryGetValue("type", out var t) ? t.AsString : "expense",
        Amount           = d.TryGetValue("amount", out var amt) ? (decimal)amt.ToDouble() : 0m,
        Date             = d.TryGetValue("date", out var dt) ? dt.ToUniversalTime() : DateTime.UtcNow,
        Category         = d.TryGetValue("category", out var cat) ? cat.AsString : "",
        PaymentMethod    = d.TryGetValue("paymentMethod", out var pm) ? pm.AsString : "",
        Description      = d.TryGetValue("description", out var desc) && !desc.IsBsonNull ? desc.AsString : null,
        Notes            = d.TryGetValue("notes", out var notes) && !notes.IsBsonNull ? notes.AsString : null,
        ReceiptUrl       = d.TryGetValue("receiptUrl", out var ru) && !ru.IsBsonNull ? ru.AsString : null,
        IsRecurring      = d.TryGetValue("isRecurring", out var ir) && ir.AsBoolean,
        CreatedAt        = d.TryGetValue("createdAt", out var ca) ? ca.ToUniversalTime() : DateTime.UtcNow,
        OriginalAmount   = d.TryGetValue("originalAmount", out var oa) && !oa.IsBsonNull ? (decimal?)oa.ToDouble() : null,
        OriginalCurrency = d.TryGetValue("originalCurrency", out var oc) && !oc.IsBsonNull ? oc.AsString : null,
        FxRate           = d.TryGetValue("fxRate", out var fx) && !fx.IsBsonNull ? (decimal?)fx.ToDouble() : null,
        ReceiptGroupId   = d.TryGetValue("receiptGroupId", out var rg) && !rg.IsBsonNull ? rg.AsString : null,
        ReceiptNumber    = d.TryGetValue("receiptNumber", out var rn) && !rn.IsBsonNull ? rn.AsString : null,
        IsReceiptItem    = d.TryGetValue("isReceiptItem", out var iri) && !iri.IsBsonNull && iri.AsBoolean,
        TaxAmount        = d.TryGetValue("taxAmount", out var ta) && !ta.IsBsonNull ? (decimal?)ta.ToDouble() : null,
    };

    public async Task<ExpenseDto> GetExpenseByIdAsync(string userId, string expenseId)
    {
        var expense = await _context.Expenses
            .Find(e => e.Id == expenseId)
            .FirstOrDefaultAsync();

        if (expense == null)
            throw new KeyNotFoundException("Expense not found");

        return MapToExpenseDto(expense);
    }

    public async Task<ExpenseDto> CreateExpenseAsync(string userId, CreateExpenseRequest request)
    {
        // If this is a recurring expense, only create RecurringExpense + upcoming payments.
        // Do NOT create an expense entry — expense will be created only when "Paid" is clicked.
        if (request.IsRecurring && request.RecurringConfig != null)
        {
            var recurring = new RecurringExpense
            {
                UserId = userId,
                ExpenseBookId = request.ExpenseBookId,
                Amount = request.Amount,
                Category = request.Category,
                PaymentMethod = request.PaymentMethod,
                Description = request.Description,
                Frequency = request.RecurringConfig.Frequency,
                StartDate = request.RecurringConfig.StartDate,
                EndDate = request.RecurringConfig.EndDate,
                NextOccurrence = request.RecurringConfig.StartDate // First occurrence is the start date itself
            };

            await _context.RecurringExpenses.InsertOneAsync(recurring);

            // Generate upcoming payment entries
            await GenerateUpcomingPaymentsForRecurringAsync(recurring);

            // Return a DTO representing the recurring setup (no actual expense was created)
            return new ExpenseDto
            {
                Id = recurring.Id,
                ExpenseBookId = recurring.ExpenseBookId,
                Amount = recurring.Amount,
                Date = recurring.StartDate,
                Category = recurring.Category,
                PaymentMethod = recurring.PaymentMethod,
                Description = recurring.Description,
                IsRecurring = true,
                CreatedAt = recurring.CreatedAt
            };
        }

        // Enforce monthly expense limit across all books
        var owner = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
        var maxPerMonth = PlanLimits.MaxExpensesPerMonth(owner?.Plan ?? PlanType.Free);
        if (maxPerMonth != int.MaxValue)
        {
            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthEnd   = monthStart.AddMonths(1);
            var monthCount = await _context.Expenses.CountDocumentsAsync(e =>
                e.UserId == userId &&
                e.Date   >= monthStart &&
                e.Date   < monthEnd);

            if (monthCount >= maxPerMonth)
                throw new InvalidOperationException(
                    $"Your {owner?.Plan.ToString() ?? "current"} plan is limited to {maxPerMonth} expenses per month. Upgrade to add more.");
        }

        // Non-recurring: create the expense entry as usual
        var finalAmount = request.Amount;
        decimal? originalAmount = null;
        string? originalCurrency = null;
        decimal? fxRate = null;

        if (!string.IsNullOrEmpty(request.OriginalCurrency) && request.OriginalAmount.HasValue)
        {
            var expBook = await _context.ExpenseBooks
                .Find(b => b.Id == request.ExpenseBookId).FirstOrDefaultAsync();
            var bookCurrency = expBook?.Currency ?? string.Empty;

            if (!string.Equals(request.OriginalCurrency, bookCurrency, StringComparison.OrdinalIgnoreCase))
            {
                var rate = await _fx.GetRateAsync(request.OriginalCurrency, bookCurrency, request.Date);
                if (rate.HasValue)
                {
                    originalAmount   = request.OriginalAmount;
                    originalCurrency = request.OriginalCurrency.ToUpperInvariant();
                    fxRate           = rate.Value;
                    finalAmount      = Math.Round(request.OriginalAmount.Value * rate.Value, 2);
                }
                // If no rate configured, fall through and use Amount as supplied by the client
            }
        }

        var expense = new Expense
        {
            UserId           = userId,
            ExpenseBookId    = request.ExpenseBookId,
            Type             = string.IsNullOrEmpty(request.Type) ? "expense" : request.Type,
            Amount           = finalAmount,
            OriginalAmount   = originalAmount,
            OriginalCurrency = originalCurrency,
            FxRate           = fxRate,
            Date             = request.Date,
            Category         = request.Category,
            PaymentMethod    = request.PaymentMethod,
            Description      = string.IsNullOrWhiteSpace(request.Description)
                                   ? await ResolveCategoryNameAsync(request.Category, request.ExpenseBookId)
                                   : request.Description,
            Notes            = request.Notes,
            IsRecurring      = false,
            ReceiptGroupId   = request.ReceiptGroupId,
            ReceiptNumber    = request.ReceiptNumber,
            IsReceiptItem    = request.IsReceiptItem,
            TaxAmount        = request.TaxAmount,
        };

        await _context.Expenses.InsertOneAsync(expense);

        // When user adds their first real expense to the demo book, graduate it out of demo mode
        // so subsequent AI chat turns are charged against the user's real credit balance.
        var book = await _context.ExpenseBooks
            .Find(eb => eb.Id == request.ExpenseBookId)
            .FirstOrDefaultAsync();
        if (book?.IsTemplate == true)
        {
            await _context.ExpenseBooks.UpdateOneAsync(
                eb => eb.Id == request.ExpenseBookId,
                Builders<ExpenseBook>.Update.Set(eb => eb.IsTemplate, false));
        }

        // Update daily expense summary
        await UpdateDailyExpenseSummaryAsync(userId, request.ExpenseBookId, expense.Date, expense.Category, expense.Amount, isAdd: true);

        // Invalidate budget cache for the month this expense falls in
        await _cache.RemoveAsync(CacheKeys.UserBudgets(userId, request.ExpenseBookId, CacheKeys.MonthKey(expense.Date)));

        return MapToExpenseDto(expense);
    }

    public async Task<List<ExpenseDto>> CreateExpenseBatchAsync(string userId, CreateExpenseBatchRequest request)
    {
        var receiptGroupId = ObjectId.GenerateNewId().ToString();
        var expenses = new List<Expense>();

        foreach (var item in request.Items)
        {
            var resolvedCategoryName = await ResolveCategoryNameAsync(item.Category, request.ExpenseBookId);
            expenses.Add(new Expense
            {
                UserId        = userId,
                ExpenseBookId = request.ExpenseBookId,
                Type          = "expense",
                Amount        = item.Amount,
                Date          = request.Date,
                Category      = resolvedCategoryName,
                PaymentMethod = request.PaymentMethod,
                Description   = string.IsNullOrWhiteSpace(item.Name) ? resolvedCategoryName : item.Name,
                IsRecurring   = false,
                IsReceiptItem = true,
                ReceiptGroupId = receiptGroupId,
                ReceiptNumber  = request.ReceiptNumber,
            });
        }

        // Tax becomes its own entry (Option C)
        if (request.TaxAmount is > 0)
        {
            var taxCategory = "Tax & Fees";
            expenses.Add(new Expense
            {
                UserId         = userId,
                ExpenseBookId  = request.ExpenseBookId,
                Type           = "expense",
                Amount         = request.TaxAmount.Value,
                Date           = request.Date,
                Category       = taxCategory,
                PaymentMethod  = request.PaymentMethod,
                Description    = string.IsNullOrWhiteSpace(request.TaxLabel) ? taxCategory : request.TaxLabel,
                IsRecurring    = false,
                IsReceiptItem  = true,
                ReceiptGroupId = receiptGroupId,
                ReceiptNumber  = request.ReceiptNumber,
                TaxAmount      = request.TaxAmount,
            });
        }

        await _context.Expenses.InsertManyAsync(expenses);

        // Graduate demo book on first real expense
        if (!string.IsNullOrEmpty(request.ExpenseBookId))
        {
            var book = await _context.ExpenseBooks.Find(eb => eb.Id == request.ExpenseBookId).FirstOrDefaultAsync();
            if (book?.IsTemplate == true)
                await _context.ExpenseBooks.UpdateOneAsync(
                    eb => eb.Id == request.ExpenseBookId,
                    Builders<ExpenseBook>.Update.Set(eb => eb.IsTemplate, false));
        }

        // Update daily summaries + invalidate budget caches
        var invalidationKeys = new HashSet<string>();
        foreach (var e in expenses)
        {
            await UpdateDailyExpenseSummaryAsync(userId, request.ExpenseBookId, e.Date, e.Category, e.Amount, isAdd: true);
            invalidationKeys.Add(CacheKeys.UserBudgets(userId, request.ExpenseBookId, CacheKeys.MonthKey(e.Date)));
        }
        await Task.WhenAll(invalidationKeys.Select(k => _cache.RemoveAsync(k)));

        return expenses.Select(MapToExpenseDto).ToList();
    }

    public async Task<ExpenseDto> UpdateExpenseAsync(string userId, string expenseId, UpdateExpenseRequest request)
    {
        var expense = await _context.Expenses
            .Find(e => e.Id == expenseId)
            .FirstOrDefaultAsync();

        if (expense == null)
            throw new KeyNotFoundException("Expense not found");

        var oldAmount = expense.Amount;
        var oldDate = expense.Date;
        var oldCategory = expense.Category;

        if (request.Amount.HasValue)
            expense.Amount = request.Amount.Value;
        if (request.Date.HasValue)
            expense.Date = request.Date.Value;
        if (!string.IsNullOrEmpty(request.Category))
            expense.Category = await ResolveCategoryNameAsync(request.Category, expense.ExpenseBookId);
        if (!string.IsNullOrEmpty(request.PaymentMethod))
            expense.PaymentMethod = request.PaymentMethod;
        if (request.Description != null)
        {
            // Empty string means "clear it" — fall back to the (possibly just-updated) category name
            expense.Description = string.IsNullOrWhiteSpace(request.Description)
                ? await ResolveCategoryNameAsync(expense.Category, expense.ExpenseBookId)
                : request.Description;
        }
        if (request.Notes != null)
            expense.Notes = request.Notes;
        if (!string.IsNullOrEmpty(request.Type))
            expense.Type = request.Type;

        expense.UpdatedAt = DateTime.UtcNow;

        await _context.Expenses.ReplaceOneAsync(e => e.Id == expenseId, expense);

        // Update daily expense summaries
        // Remove old amount from old date/category
        await UpdateDailyExpenseSummaryAsync(userId, expense.ExpenseBookId, oldDate, oldCategory, oldAmount, isAdd: false);
        // Add new amount to new date/category
        await UpdateDailyExpenseSummaryAsync(userId, expense.ExpenseBookId, expense.Date, expense.Category, expense.Amount, isAdd: true);

        // Invalidate budget cache — if the date changed month, invalidate both months
        var invalidations = new List<Task>
        {
            _cache.RemoveAsync(CacheKeys.UserBudgets(userId, expense.ExpenseBookId, CacheKeys.MonthKey(expense.Date)))
        };
        if (CacheKeys.MonthKey(oldDate) != CacheKeys.MonthKey(expense.Date))
            invalidations.Add(_cache.RemoveAsync(CacheKeys.UserBudgets(userId, expense.ExpenseBookId, CacheKeys.MonthKey(oldDate))));
        await Task.WhenAll(invalidations);

        return MapToExpenseDto(expense);
    }

    public async Task<bool> DeleteExpenseAsync(string userId, string expenseId)
    {
        var expense = await _context.Expenses
            .Find(e => e.Id == expenseId)
            .FirstOrDefaultAsync();

        if (expense == null)
            return false;

        // Update daily expense summary before deleting
        await UpdateDailyExpenseSummaryAsync(userId, expense.ExpenseBookId, expense.Date, expense.Category, expense.Amount, isAdd: false);

        var result = await _context.Expenses
            .DeleteOneAsync(e => e.Id == expenseId);

        if (result.DeletedCount > 0)
            await _cache.RemoveAsync(CacheKeys.UserBudgets(userId, expense.ExpenseBookId, CacheKeys.MonthKey(expense.Date)));

        return result.DeletedCount > 0;
    }

    public async Task<string> UploadReceiptAsync(string userId, string expenseId, Stream fileStream, string fileName)
    {
        var expense = await _context.Expenses
            .Find(e => e.Id == expenseId && e.UserId == userId)
            .FirstOrDefaultAsync();

        if (expense == null)
            throw new KeyNotFoundException("Expense not found");

        // TODO: Implement actual file upload to cloud storage (Azure Blob, AWS S3, etc.)
        var receiptUrl = $"/uploads/receipts/{expenseId}_{fileName}";

        expense.ReceiptUrl = receiptUrl;
        expense.UpdatedAt = DateTime.UtcNow;

        await _context.Expenses.ReplaceOneAsync(e => e.Id == expenseId, expense);

        return receiptUrl;
    }

    private async Task<string> ResolveCategoryNameAsync(string categoryIdOrName, string? expenseBookId)
    {
        if (string.IsNullOrWhiteSpace(categoryIdOrName)) return string.Empty;
        var cat = await _context.Categories
            .Find(c => (c.Id == categoryIdOrName || c.Name == categoryIdOrName)
                    && (string.IsNullOrEmpty(expenseBookId) || c.ExpenseBookId == expenseBookId))
            .FirstOrDefaultAsync();
        return cat?.Name ?? categoryIdOrName;
    }

    private DateTime CalculateNextOccurrence(DateTime startDate, string frequency)
    {
        return frequency.ToLower() switch
        {
            "daily" => startDate.AddDays(1),
            "weekly" => startDate.AddDays(7),
            "monthly" => startDate.AddMonths(1),
            "yearly" => startDate.AddYears(1),
            _ => startDate.AddMonths(1)
        };
    }

    private async Task UpdateDailyExpenseSummaryAsync(string userId, string? expenseBookId, DateTime expenseDate, string category, decimal amount, bool isAdd)
    {
        var dateOnly = expenseDate.Date; // Remove time component

        var filterBuilder = Builders<DailyExpenseSummary>.Filter;
        var filter = filterBuilder.Eq(d => d.UserId, userId) &
                     filterBuilder.Eq(d => d.Date, dateOnly);
        
        if (!string.IsNullOrEmpty(expenseBookId))
        {
            filter &= filterBuilder.Eq(d => d.ExpenseBookId, expenseBookId);
        }

        var summary = await _context.DailyExpenseSummaries.Find(filter).FirstOrDefaultAsync();

        if (summary == null && isAdd)
        {
            // Create new summary for this date
            summary = new DailyExpenseSummary
            {
                UserId = userId,
                ExpenseBookId = expenseBookId,
                Date = dateOnly,
                CategorySpending = new List<CategorySpending>
                {
                    new CategorySpending
                    {
                        Category = category,
                        Amount = amount,
                        Count = 1
                    }
                },
                TotalSpent = amount
            };

            await _context.DailyExpenseSummaries.InsertOneAsync(summary);
        }
        else if (summary != null)
        {
            // Update existing summary
            var categorySpending = summary.CategorySpending.FirstOrDefault(c => c.Category == category);

            if (categorySpending != null)
            {
                if (isAdd)
                {
                    categorySpending.Amount += amount;
                    categorySpending.Count++;
                }
                else
                {
                    categorySpending.Amount -= amount;
                    categorySpending.Count--;

                    // Remove category if count is 0
                    if (categorySpending.Count <= 0)
                    {
                        summary.CategorySpending.Remove(categorySpending);
                    }
                }
            }
            else if (isAdd)
            {
                // Add new category spending
                summary.CategorySpending.Add(new CategorySpending
                {
                    Category = category,
                    Amount = amount,
                    Count = 1
                });
            }

            // Update total spent
            summary.TotalSpent = isAdd ? summary.TotalSpent + amount : summary.TotalSpent - amount;
            summary.UpdatedAt = DateTime.UtcNow;

            // Sort category spending by amount descending
            summary.CategorySpending = summary.CategorySpending.OrderByDescending(c => c.Amount).ToList();

            if (summary.CategorySpending.Count == 0 && summary.TotalSpent <= 0)
            {
                // Delete summary if no more expenses
                await _context.DailyExpenseSummaries.DeleteOneAsync(filter);
            }
            else
            {
                await _context.DailyExpenseSummaries.ReplaceOneAsync(filter, summary);
            }
        }
    }

    private ExpenseDto MapToExpenseDto(Expense expense)
    {
        return new ExpenseDto
        {
            Id               = expense.Id,
            ExpenseBookId    = expense.ExpenseBookId,
            Type             = string.IsNullOrEmpty(expense.Type) ? "expense" : expense.Type,
            Amount           = expense.Amount,
            Date             = expense.Date,
            Category         = expense.Category,
            PaymentMethod    = expense.PaymentMethod,
            Description      = expense.Description,
            Notes            = expense.Notes,
            ReceiptUrl       = expense.ReceiptUrl,
            IsRecurring      = expense.IsRecurring,
            CreatedAt        = expense.CreatedAt,
            OriginalAmount   = expense.OriginalAmount,
            OriginalCurrency = expense.OriginalCurrency,
            FxRate           = expense.FxRate,
            ReceiptGroupId   = expense.ReceiptGroupId,
            ReceiptNumber    = expense.ReceiptNumber,
            IsReceiptItem    = expense.IsReceiptItem,
            TaxAmount        = expense.TaxAmount,
        };
    }

    private RecurringExpenseDto MapRecurringExpenseToDto(RecurringExpense recurring)
    {
        return new RecurringExpenseDto
        {
            Id = recurring.Id,
            ExpenseBookId = recurring.ExpenseBookId,
            Amount = recurring.Amount,
            Category = recurring.Category,
            PaymentMethod = recurring.PaymentMethod,
            Description = recurring.Description,
            Frequency = recurring.Frequency,
            StartDate = recurring.StartDate,
            EndDate = recurring.EndDate,
            NextOccurrence = recurring.NextOccurrence,
            LastProcessed = recurring.LastProcessed,
            IsActive = recurring.IsActive,
            CreatedAt = recurring.CreatedAt,
            UpdatedAt = recurring.UpdatedAt
        };
    }

    public async Task<List<RecurringExpenseDto>> GetRecurringExpensesAsync(string userId, string? expenseBookId, DateTime? startDate, DateTime? endDate)
    {
        var filterBuilder = Builders<RecurringExpense>.Filter;
        var filter = filterBuilder.Eq(r => r.UserId, userId) & filterBuilder.Eq(r => r.IsActive, true);

        if (!string.IsNullOrEmpty(expenseBookId))
            filter &= filterBuilder.Eq(r => r.ExpenseBookId, expenseBookId);

        // Filter by NextOccurrence falling within the date range
        if (startDate.HasValue)
            filter &= filterBuilder.Gte(r => r.NextOccurrence, startDate.Value);

        if (endDate.HasValue)
            filter &= filterBuilder.Lte(r => r.NextOccurrence, endDate.Value);

        var recurringExpenses = await _context.RecurringExpenses
            .Find(filter)
            .SortBy(r => r.NextOccurrence)
            .ToListAsync();

        return recurringExpenses.Select(MapRecurringExpenseToDto).ToList();
    }

    public async Task<ExpenseDto> MarkRecurringExpenseAsPaidAsync(string userId, string recurringExpenseId, DateTime paidDate)
    {
        var recurring = await _context.RecurringExpenses
            .Find(r => r.Id == recurringExpenseId && r.UserId == userId)
            .FirstOrDefaultAsync();

        if (recurring == null)
            throw new KeyNotFoundException("Recurring expense not found");

        // Find and remove the earliest unpaid upcoming payment for this recurring expense
        var upcomingPayment = await _context.UpcomingPayments
            .Find(u => u.RecurringExpenseId == recurringExpenseId && u.UserId == userId && u.Status != "paid")
            .SortBy(u => u.DueDate)
            .FirstOrDefaultAsync();

        if (upcomingPayment != null)
        {
            await _context.UpcomingPayments.DeleteOneAsync(u => u.Id == upcomingPayment.Id);
        }

        var expenseDate = upcomingPayment?.DueDate ?? paidDate;

        // Create a new expense for this payment
        var expense = new Expense
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            UserId = userId,
            ExpenseBookId = recurring.ExpenseBookId,
            Amount = recurring.Amount,
            Date = expenseDate,
            Category = recurring.Category,
            PaymentMethod = recurring.PaymentMethod,
            Description = recurring.Description,
            Notes = $"Payment for recurring: {recurring.Description}",
            IsRecurring = true,
            RecurringId = recurringExpenseId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Expenses.InsertOneAsync(expense);

        // Update the recurring expense's last processed date and next occurrence
        recurring.LastProcessed = paidDate;
        recurring.NextOccurrence = CalculateNextOccurrence(expenseDate, recurring.Frequency);
        recurring.UpdatedAt = DateTime.UtcNow;

        await _context.RecurringExpenses.ReplaceOneAsync(r => r.Id == recurringExpenseId, recurring);

        // Update daily expense summary
        await UpdateDailyExpenseSummaryAsync(userId, recurring.ExpenseBookId, expenseDate, recurring.Category, recurring.Amount, isAdd: true);

        // Invalidate budget cache for the month this payment falls in
        await _cache.RemoveAsync(CacheKeys.UserBudgets(userId, recurring.ExpenseBookId, CacheKeys.MonthKey(expenseDate)));

        // Generate new upcoming payments to maintain 2 instances
        await GenerateUpcomingPaymentsForRecurringAsync(recurring);

        return MapToExpenseDto(expense);
    }

    /// <summary>
    /// Generate 2 upcoming payment entries for a recurring expense.
    /// </summary>
    public async Task GenerateUpcomingPaymentsForRecurringAsync(RecurringExpense recurring)
    {
        // Check how many upcoming payments already exist for this recurring expense
        var existingCount = await _context.UpcomingPayments
            .Find(u => u.RecurringExpenseId == recurring.Id)
            .CountDocumentsAsync();

        // We want to maintain exactly 2 upcoming payment instances
        var instancesNeeded = 2 - (int)existingCount;

        if (instancesNeeded <= 0)
            return; // Already have 2 or more instances

        var currentDate = recurring.NextOccurrence;

        for (int i = 0; i < instancesNeeded; i++)
        {
            // Check if end date is set and we've passed it
            if (recurring.EndDate.HasValue && currentDate > recurring.EndDate.Value)
                break;

            // Check if this upcoming payment already exists
            var exists = await _context.UpcomingPayments
                .Find(u => u.RecurringExpenseId == recurring.Id && u.DueDate == currentDate)
                .AnyAsync();

            if (!exists)
            {
                var status = GetUpcomingPaymentStatus(currentDate);

                var upcomingPayment = new UpcomingPayment
                {
                    UserId = recurring.UserId,
                    ExpenseBookId = recurring.ExpenseBookId,
                    RecurringExpenseId = recurring.Id,
                    Amount = recurring.Amount,
                    Category = recurring.Category,
                    PaymentMethod = recurring.PaymentMethod,
                    Description = recurring.Description,
                    Frequency = recurring.Frequency,
                    DueDate = currentDate,
                    Status = status
                };

                await _context.UpcomingPayments.InsertOneAsync(upcomingPayment);
            }

            currentDate = CalculateNextOccurrence(currentDate, recurring.Frequency);
        }
    }

    /// <summary>
    /// Generate upcoming payments for ALL active recurring expenses for a user.
    /// </summary>
    public async Task GenerateAllUpcomingPaymentsAsync(string userId)
    {
        var activeRecurring = await _context.RecurringExpenses
            .Find(r => r.UserId == userId && r.IsActive == true)
            .ToListAsync();

        foreach (var recurring in activeRecurring)
        {
            await GenerateUpcomingPaymentsForRecurringAsync(recurring);
        }
    }

    /// <summary>
    /// Refresh statuses of all upcoming payments for a user based on current date.
    /// </summary>
    public async Task RefreshUpcomingPaymentStatusesAsync(string userId)
    {
        var upcomingPayments = await _context.UpcomingPayments
            .Find(u => u.UserId == userId)
            .ToListAsync();

        foreach (var payment in upcomingPayments)
        {
            var newStatus = GetUpcomingPaymentStatus(payment.DueDate);
            if (newStatus != payment.Status)
            {
                payment.Status = newStatus;
                payment.UpdatedAt = DateTime.UtcNow;
                await _context.UpcomingPayments.ReplaceOneAsync(u => u.Id == payment.Id, payment);
            }
        }
    }

    private string GetUpcomingPaymentStatus(DateTime dueDate)
    {
        var today = DateTime.UtcNow.Date;
        var daysUntilDue = (dueDate.Date - today).Days;

        if (daysUntilDue < -7)
            return "pending"; // Unpaid for more than a week
        if (daysUntilDue < 0)
            return "overdue"; // Past due date
        if (daysUntilDue == 0)
            return "due"; // Due today
        return "upcoming"; // Due in the future
    }

    /// <summary>
    /// Delete all upcoming payments for a recurring expense.
    /// </summary>
    public async Task DeleteUpcomingPaymentsForRecurringAsync(string recurringExpenseId)
    {
        await _context.UpcomingPayments.DeleteManyAsync(u => u.RecurringExpenseId == recurringExpenseId);
    }

    /// <summary>
    /// Update a recurring expense and regenerate upcoming payments.
    /// </summary>
    public async Task<RecurringExpenseDto> UpdateRecurringExpenseAsync(
        string userId, 
        string recurringExpenseId, 
        UpdateRecurringExpenseRequest request)
    {
        var recurring = await _context.RecurringExpenses
            .Find(r => r.Id == recurringExpenseId && r.UserId == userId)
            .FirstOrDefaultAsync();

        if (recurring == null)
            throw new KeyNotFoundException("Recurring expense not found");

        // Track if critical fields changed (amount, frequency, startDate)
        bool needRegenerate = false;

        if (request.Amount.HasValue && request.Amount.Value != recurring.Amount)
        {
            recurring.Amount = request.Amount.Value;
            needRegenerate = true;
        }

        if (!string.IsNullOrEmpty(request.Frequency) && request.Frequency != recurring.Frequency)
        {
            recurring.Frequency = request.Frequency;
            needRegenerate = true;
        }

        if (request.StartDate.HasValue && request.StartDate.Value != recurring.StartDate)
        {
            recurring.StartDate = request.StartDate.Value;
            recurring.NextOccurrence = request.StartDate.Value;
            needRegenerate = true;
        }

        if (!string.IsNullOrEmpty(request.Category))
            recurring.Category = request.Category;

        if (!string.IsNullOrEmpty(request.PaymentMethod))
            recurring.PaymentMethod = request.PaymentMethod;

        if (request.Description != null)
            recurring.Description = request.Description;

        if (request.EndDate.HasValue)
            recurring.EndDate = request.EndDate;

        recurring.UpdatedAt = DateTime.UtcNow;

        // Update the recurring expense
        await _context.RecurringExpenses.ReplaceOneAsync(r => r.Id == recurringExpenseId, recurring);

        // If critical fields changed, delete all unpaid upcoming payments and create 2 new ones
        if (needRegenerate)
        {
            await DeleteUpcomingPaymentsForRecurringAsync(recurringExpenseId);
            await GenerateUpcomingPaymentsForRecurringAsync(recurring);
        }

        return MapRecurringExpenseToDto(recurring);
    }

    /// <summary>
    /// Delete a recurring expense (soft delete by marking inactive) and remove upcoming payments.
    /// </summary>
    public async Task<bool> DeleteRecurringExpenseAsync(string userId, string recurringExpenseId)
    {
        var recurring = await _context.RecurringExpenses
            .Find(r => r.Id == recurringExpenseId && r.UserId == userId)
            .FirstOrDefaultAsync();

        if (recurring == null)
            return false;

        // Mark as inactive instead of hard delete
        recurring.IsActive = false;
        recurring.UpdatedAt = DateTime.UtcNow;
        await _context.RecurringExpenses.ReplaceOneAsync(r => r.Id == recurringExpenseId, recurring);

        // Delete all upcoming payments for this recurring expense
        await DeleteUpcomingPaymentsForRecurringAsync(recurringExpenseId);

        return true;
    }
}