using ExpensesBackend.API.Domain;
using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Domain.Entities;
using ExpensesBackend.API.Infrastructure.Data;
using ExpensesBackend.API.Services.Interfaces;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace ExpensesBackend.API.Services.Admin;

public class AdminUserService : IAdminUserService
{
    private readonly MongoDbContext _ctx;

    public AdminUserService(MongoDbContext ctx) => _ctx = ctx;

    public async Task<AdminUserListDto> GetUsersAsync(string? search, int page, int pageSize)
    {
        FilterDefinition<User> filter;
        if (string.IsNullOrWhiteSpace(search))
        {
            filter = FilterDefinition<User>.Empty;
        }
        else
        {
            // Escape special regex chars so raw user input (e.g. email dots, plus signs) is treated literally.
            // Only search email — email has a DB index; name does not, and an unindexed $regex throws on Cosmos DB.
            var escaped = Regex.Escape(search.Trim());
            filter = Builders<User>.Filter.Regex(u => u.Email, new MongoDB.Bson.BsonRegularExpression(escaped, "i"));
        }

        var total = await _ctx.Users.CountDocumentsAsync(filter);
        var users = await _ctx.Users.Find(filter)
            .SortByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        var summaries = new List<AdminUserSummaryDto>();
        foreach (var user in users)
        {
            var bookCount = (int)await _ctx.ExpenseBooks.CountDocumentsAsync(
                Builders<ExpenseBook>.Filter.Eq(b => b.UserId, user.Id)
                & Builders<ExpenseBook>.Filter.Eq(b => b.IsTemplate, false));

            summaries.Add(new AdminUserSummaryDto
            {
                Id        = user.Id,
                Email     = user.Email,
                Name      = user.Name,
                Plan      = user.Plan.ToString(),
                IsActive  = true, // tracked via deactivation flag to be added
                CreatedAt = user.CreatedAt,
                BookCount = bookCount,
            });
        }

        return new AdminUserListDto { Users = summaries, Total = total, Page = page, PageSize = pageSize };
    }

    public async Task<AdminUserDetailDto?> GetUserDetailAsync(string userId)
    {
        var user = await _ctx.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
        if (user == null) return null;

        var books = await _ctx.ExpenseBooks
            .Find(b => b.UserId == userId && !b.IsTemplate)
            .SortByDescending(b => b.CreatedAt)
            .ToListAsync();

        var subscription = await _ctx.UserSubscriptions
            .Find(s => s.UserId == userId && s.Status == "active")
            .FirstOrDefaultAsync();

        var bookIds = books.Select(b => b.Id).ToList();
        var bookCredits = bookIds.Count == 0
            ? []
            : await _ctx.BookCredits
                .Find(Builders<BookCredits>.Filter.In(bc => bc.ExpenseBookId, bookIds))
                .ToListAsync();

        return new AdminUserDetailDto
        {
            Id       = user.Id,
            Email    = user.Email,
            Name     = user.Name,
            Currency = user.Currency,
            Plan     = user.Plan.ToString(),
            IsActive = true,
            CreatedAt = user.CreatedAt,
            Books = books.Select(b => new AdminUserBookDto
            {
                Id             = b.Id,
                Name           = b.Name,
                IsDefault      = b.IsDefault,
                AiChatEnabled  = b.AiChatEnabled,
                ExpenseCount   = b.ExpenseCount,
                CreatedAt      = b.CreatedAt,
            }).ToList(),
            Subscription = subscription == null ? null : new AdminUserSubscriptionDto
            {
                Status            = subscription.Status,
                Plan              = subscription.Plan.ToString(),
                CancelAtPeriodEnd = subscription.CancelAtPeriodEnd,
                CurrentPeriodEnd  = subscription.CurrentPeriodEnd,
            },
            Credits = new AdminUserCreditSummaryDto
            {
                TotalFreeLeft = bookCredits.Sum(bc => bc.FreeCreditsLeft),
                TotalPaidLeft = bookCredits.Sum(bc => bc.PaidCreditsLeft),
            },
        };
    }

    public async Task<AdminUserDetailDto> ChangePlanAsync(string userId, string plan, string adminId, string adminEmail)
    {
        if (!Enum.TryParse<PlanType>(plan, ignoreCase: true, out var planType))
            throw new ArgumentException($"Invalid plan: {plan}");

        var user = await _ctx.Users.Find(u => u.Id == userId).FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("User not found.");

        var oldPlan = user.Plan.ToString();

        await _ctx.Users.UpdateOneAsync(
            u => u.Id == userId,
            Builders<User>.Update
                .Set(u => u.Plan,      planType)
                .Set(u => u.UpdatedAt, DateTime.UtcNow));

        await _ctx.AdminAuditLogs.InsertOneAsync(new AdminAuditLog
        {
            AdminId    = adminId,
            AdminEmail = adminEmail,
            Action     = AdminActions.ChangePlan,
            TargetType = "user",
            TargetId   = userId,
            Payload    = new Dictionary<string, object> { ["oldPlan"] = oldPlan, ["newPlan"] = plan },
            Summary    = $"Changed plan from {oldPlan} to {plan} for {user.Email}",
            Timestamp  = DateTime.UtcNow,
        });

        return (await GetUserDetailAsync(userId))!;
    }

    public async Task<AdminUserDetailDto> SetActiveAsync(string userId, bool isActive, string adminId, string adminEmail)
    {
        var user = await _ctx.Users.Find(u => u.Id == userId).FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("User not found.");

        // IsActive is a future field — for now, store the intent in the audit log
        // and return the current detail. Field addition to User entity handled separately.
        var action = isActive ? AdminActions.ReactivateUser : AdminActions.DeactivateUser;

        await _ctx.AdminAuditLogs.InsertOneAsync(new AdminAuditLog
        {
            AdminId    = adminId,
            AdminEmail = adminEmail,
            Action     = action,
            TargetType = "user",
            TargetId   = userId,
            Summary    = $"{(isActive ? "Reactivated" : "Deactivated")} user {user.Email}",
            Timestamp  = DateTime.UtcNow,
        });

        return (await GetUserDetailAsync(userId))!;
    }
}
