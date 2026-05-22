using ExpensesBackend.API.Domain;
using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;

namespace ExpensesBackend.API.Controllers;

[Authorize]
[ApiController]
[Route("api/usage")]
public class UsageController : ControllerBase
{
    private readonly MongoDbContext _context;

    public UsageController(MongoDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<UsageDto>>> GetUsage()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var user = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
        var plan = user?.Plan ?? PlanType.Free;

        var booksOwned = (int)await _context.ExpenseBooks
            .CountDocumentsAsync(eb => eb.UserId == userId);

        var now        = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd   = monthStart.AddMonths(1);

        var expensesThisMonth = (int)await _context.Expenses
            .CountDocumentsAsync(e => e.UserId == userId && e.Date >= monthStart && e.Date < monthEnd);

        var rawExpensesLimit = PlanLimits.MaxExpensesPerMonth(plan);
        var rawBooksLimit    = PlanLimits.MaxBooks(plan);

        // Category usage only relevant for Free plan (global limit shown in account settings)
        int categoriesUsed  = 0;
        int categoriesLimit = -1;
        if (plan == PlanType.Free)
        {
            var memberDocs  = await _context.ExpenseBookMembers
                .Find(m => m.UserId == userId && !m.IsDeleted)
                .ToListAsync();
            categoriesUsed  = memberDocs.Sum(m => m.CategoriesUsed);
            categoriesLimit = PlanLimits.MaxCategories(PlanType.Free);
        }

        return Ok(ApiResponse<UsageDto>.SuccessResponse(new UsageDto
        {
            BooksOwned        = booksOwned,
            BooksLimit        = rawBooksLimit    == int.MaxValue ? -1 : rawBooksLimit,
            ExpensesThisMonth = expensesThisMonth,
            ExpensesLimit     = rawExpensesLimit == int.MaxValue ? -1 : rawExpensesLimit,
            CategoriesUsed   = categoriesUsed,
            CategoriesLimit  = categoriesLimit,
        }));
    }
}
