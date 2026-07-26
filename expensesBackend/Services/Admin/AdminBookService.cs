using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Domain.Entities;
using ExpensesBackend.API.Infrastructure.Data;
using ExpensesBackend.API.Services.Interfaces;
using MongoDB.Driver;

namespace ExpensesBackend.API.Services.Admin;

public class AdminBookService : IAdminBookService
{
    private readonly MongoDbContext _ctx;

    public AdminBookService(MongoDbContext ctx) => _ctx = ctx;

    public async Task<AdminBookListDto> GetBooksAsync(string? search, bool? aiEnabled, string? userId, int page, int pageSize)
    {
        var filterBuilder = Builders<ExpenseBook>.Filter;
        var filter = filterBuilder.Eq(b => b.IsTemplate, false);

        if (!string.IsNullOrWhiteSpace(userId))
            filter &= filterBuilder.Eq(b => b.UserId, userId);

        if (!string.IsNullOrWhiteSpace(search))
            filter &= filterBuilder.Regex(b => b.Name, new MongoDB.Bson.BsonRegularExpression(search, "i"));

        if (aiEnabled.HasValue)
            filter &= filterBuilder.Eq(b => b.AiChatEnabled, aiEnabled.Value);

        var total = await _ctx.ExpenseBooks.CountDocumentsAsync(filter);
        var books = await _ctx.ExpenseBooks.Find(filter)
            .SortByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        var summaries = new List<AdminBookSummaryDto>();
        foreach (var book in books)
        {
            var owner = await _ctx.Users.Find(u => u.Id == book.UserId).FirstOrDefaultAsync();
            var memberCount = (int)await _ctx.ExpenseBookMembers.CountDocumentsAsync(
                Builders<ExpenseBookMember>.Filter.Eq(m => m.ExpenseBookId, book.Id)
                & Builders<ExpenseBookMember>.Filter.Eq(m => m.InviteStatus, "accepted"));

            summaries.Add(new AdminBookSummaryDto
            {
                Id             = book.Id,
                Name           = book.Name,
                OwnerEmail     = owner?.Email ?? string.Empty,
                OwnerPlan      = (owner?.Plan ?? Domain.PlanType.Free).ToString(),
                AiChatEnabled  = book.AiChatEnabled,
                IsTemplate     = book.IsTemplate,
                ExpenseCount   = book.ExpenseCount,
                MemberCount    = memberCount,
                CreatedAt      = book.CreatedAt,
            });
        }

        return new AdminBookListDto { Books = summaries, Total = total, Page = page, PageSize = pageSize };
    }

    public async Task<AdminBookSummaryDto> ToggleAiChatAsync(string bookId, bool enabled, string adminId, string adminEmail)
    {
        var book = await _ctx.ExpenseBooks.Find(b => b.Id == bookId).FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Book not found.");

        await _ctx.ExpenseBooks.UpdateOneAsync(
            b => b.Id == bookId,
            Builders<ExpenseBook>.Update
                .Set(b => b.AiChatEnabled, enabled)
                .Set(b => b.UpdatedAt,     DateTime.UtcNow));

        var owner = await _ctx.Users.Find(u => u.Id == book.UserId).FirstOrDefaultAsync();

        await _ctx.AdminAuditLogs.InsertOneAsync(new AdminAuditLog
        {
            AdminId    = adminId,
            AdminEmail = adminEmail,
            Action     = AdminActions.ToggleAiChat,
            TargetType = "book",
            TargetId   = bookId,
            Payload    = new Dictionary<string, object> { ["enabled"] = enabled },
            Summary    = $"{(enabled ? "Enabled" : "Disabled")} AI chat for \"{book.Name}\" ({owner?.Email})",
            Timestamp  = DateTime.UtcNow,
        });

        var memberCount = (int)await _ctx.ExpenseBookMembers.CountDocumentsAsync(
            Builders<ExpenseBookMember>.Filter.Eq(m => m.ExpenseBookId, bookId)
            & Builders<ExpenseBookMember>.Filter.Eq(m => m.InviteStatus, "accepted"));

        return new AdminBookSummaryDto
        {
            Id            = book.Id,
            Name          = book.Name,
            OwnerEmail    = owner?.Email ?? string.Empty,
            OwnerPlan     = (owner?.Plan ?? Domain.PlanType.Free).ToString(),
            AiChatEnabled = enabled,
            IsTemplate    = book.IsTemplate,
            ExpenseCount  = book.ExpenseCount,
            MemberCount   = memberCount,
            CreatedAt     = book.CreatedAt,
        };
    }

    public async Task DeleteBookAsync(string bookId, string adminId, string adminEmail)
    {
        var book = await _ctx.ExpenseBooks.Find(b => b.Id == bookId).FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Book not found.");

        var owner = await _ctx.Users.Find(u => u.Id == book.UserId).FirstOrDefaultAsync();

        await _ctx.AdminAuditLogs.InsertOneAsync(new AdminAuditLog
        {
            AdminId    = adminId,
            AdminEmail = adminEmail,
            Action     = AdminActions.DeleteBook,
            TargetType = "book",
            TargetId   = bookId,
            Payload    = new Dictionary<string, object> { ["bookName"] = book.Name },
            Summary    = $"Deleted book \"{book.Name}\" owned by {owner?.Email}",
            Timestamp  = DateTime.UtcNow,
        });

        // Delete book and all associated data
        await _ctx.ExpenseBooks.DeleteOneAsync(b => b.Id == bookId);
        await _ctx.Expenses.DeleteManyAsync(e => e.ExpenseBookId == bookId);
        await _ctx.Categories.DeleteManyAsync(c => c.ExpenseBookId == bookId);
        await _ctx.Budgets.DeleteManyAsync(b => b.ExpenseBookId == bookId);
        await _ctx.ExpenseBookMembers.DeleteManyAsync(m => m.ExpenseBookId == bookId);
        await _ctx.BookCredits.DeleteOneAsync(bc => bc.ExpenseBookId == bookId);
        await _ctx.RecurringExpenses.DeleteManyAsync(r => r.ExpenseBookId == bookId);
    }
}
