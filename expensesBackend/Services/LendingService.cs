using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Domain.Entities;
using ExpensesBackend.API.Infrastructure.Data;
using ExpensesBackend.API.Services.Interfaces;
using MongoDB.Driver;

namespace ExpensesBackend.API.Services;

public class LendingService : ILendingService
{
    private readonly MongoDbContext _context;

    public LendingService(MongoDbContext context)
    {
        _context = context;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<Lending> GetOwnedLendingAsync(string userId, string expenseBookId, string lendingId)
    {
        var lending = await _context.Lendings
            .Find(l => l.Id == lendingId && l.ExpenseBookId == expenseBookId && !l.IsDeleted)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Lending not found");

        await VerifyBookOwnershipAsync(userId, expenseBookId);
        return lending;
    }

    private async Task VerifyBookOwnershipAsync(string userId, string expenseBookId)
    {
        var book = await _context.ExpenseBooks
            .Find(b => b.Id == expenseBookId && b.UserId == userId)
            .FirstOrDefaultAsync();

        if (book is null)
        {
            // Check membership (members can view but not modify — owner-only ops checked separately)
            var member = await _context.ExpenseBookMembers
                .Find(m => m.ExpenseBookId == expenseBookId && m.UserId == userId && m.InviteStatus == "accepted")
                .FirstOrDefaultAsync();
            if (member is null)
                throw new KeyNotFoundException("Expense book not found or access denied");
        }
    }

    /// <summary>
    /// Calculates accrued simple interest using reducing balance.
    /// Interest accumulates on the outstanding principal between repayments.
    /// </summary>
    private static decimal ComputeAccruedInterest(Lending lending, List<LendingRepayment> repayments)
    {
        if (lending.AnnualInterestRate <= 0)
            return 0;

        var sorted = repayments
            .Where(r => !r.IsDeleted)
            .OrderBy(r => r.Date)
            .ToList();

        decimal dailyRate = lending.AnnualInterestRate / 100m / 365m;
        decimal balance = lending.PrincipalAmount;
        decimal totalInterest = 0;
        DateTime periodStart = lending.StartDate;

        foreach (var repayment in sorted)
        {
            int days = Math.Max(0, (repayment.Date - periodStart).Days);
            totalInterest += balance * dailyRate * days;
            balance = Math.Max(0, balance - repayment.Amount);
            periodStart = repayment.Date;
        }

        // Interest on remaining balance up to today
        int remainingDays = Math.Max(0, (DateTime.UtcNow - periodStart).Days);
        totalInterest += balance * dailyRate * remainingDays;

        return Math.Round(totalInterest, 2);
    }

    /// <summary>
    /// Projected interest on the original principal over the full loan term (informational — ignores repayments).
    /// </summary>
    private static decimal ComputeProjectedTotalInterest(Lending lending)
    {
        if (lending.AnnualInterestRate <= 0 || !lending.DueDate.HasValue)
            return 0;

        int totalDays = Math.Max(0, (lending.DueDate.Value - lending.StartDate).Days);
        decimal dailyRate = lending.AnnualInterestRate / 100m / 365m;
        return Math.Round(lending.PrincipalAmount * dailyRate * totalDays, 2);
    }

    /// <summary>
    /// Future interest on the current outstanding balance from today to the due date.
    /// Zero if principal is fully repaid or no due date.
    /// </summary>
    private static decimal ComputeFutureInterest(Lending lending)
    {
        if (lending.AnnualInterestRate <= 0 || !lending.DueDate.HasValue || lending.OutstandingPrincipal <= 0)
            return 0;

        int daysRemaining = Math.Max(0, (lending.DueDate.Value.Date - DateTime.UtcNow.Date).Days);
        decimal dailyRate = lending.AnnualInterestRate / 100m / 365m;
        return Math.Round(lending.OutstandingPrincipal * dailyRate * daysRemaining, 2);
    }

    private LendingDto MapToDto(Lending lending, decimal accruedInterest)
    {
        bool isOverdue = lending.Status == "active"
            && lending.DueDate.HasValue
            && lending.DueDate.Value < DateTime.UtcNow;

        decimal projectedTotalInterest = ComputeProjectedTotalInterest(lending);
        decimal futureInterest = ComputeFutureInterest(lending);

        // TotalToRecover = what the borrower actually still owes:
        //   outstanding principal + interest already accrued + interest yet to accrue until due date
        decimal totalToRecover = lending.OutstandingPrincipal + accruedInterest + futureInterest;

        return new LendingDto
        {
            Id = lending.Id,
            ExpenseBookId = lending.ExpenseBookId,
            BorrowerName = lending.BorrowerName,
            BorrowerContact = lending.BorrowerContact,
            PrincipalAmount = lending.PrincipalAmount,
            AnnualInterestRate = lending.AnnualInterestRate,
            StartDate = lending.StartDate,
            DueDate = lending.DueDate,
            TotalRepaid = lending.TotalRepaid,
            OutstandingPrincipal = lending.OutstandingPrincipal,
            RepaymentCount = lending.RepaymentCount,
            Status = lending.Status,
            Notes = lending.Notes,
            AccruedInterest = accruedInterest,
            FutureInterest = futureInterest,
            ProjectedTotalInterest = projectedTotalInterest,
            TotalToRecover = totalToRecover,
            IsOverdue = isOverdue,
            CreatedAt = lending.CreatedAt,
            UpdatedAt = lending.UpdatedAt
        };
    }

    private static RepaymentDto MapRepaymentToDto(LendingRepayment r) => new()
    {
        Id = r.Id,
        LendingId = r.LendingId,
        Date = r.Date,
        Amount = r.Amount,
        Notes = r.Notes,
        RecordedAt = r.RecordedAt
    };

    // ── Lending CRUD ──────────────────────────────────────────────────────────

    public async Task<List<LendingDto>> GetLendingsAsync(string userId, string expenseBookId, string? status = null)
    {
        await VerifyBookOwnershipAsync(userId, expenseBookId);

        var filter = Builders<Lending>.Filter.And(
            Builders<Lending>.Filter.Eq(l => l.ExpenseBookId, expenseBookId),
            Builders<Lending>.Filter.Eq(l => l.IsDeleted, false)
        );

        if (!string.IsNullOrEmpty(status))
            filter &= Builders<Lending>.Filter.Eq(l => l.Status, status);

        var lendings = await _context.Lendings
            .Find(filter)
            .SortByDescending(l => l.CreatedAt)
            .ToListAsync();

        if (lendings.Count == 0)
            return [];

        // Batch-fetch all active repayments for interest calculation
        var lendingIds = lendings.Select(l => l.Id).ToList();
        var repayments = await _context.LendingRepayments
            .Find(r => lendingIds.Contains(r.LendingId) && !r.IsDeleted)
            .ToListAsync();

        var repaymentsByLending = repayments.GroupBy(r => r.LendingId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return lendings.Select(l =>
        {
            repaymentsByLending.TryGetValue(l.Id, out var lRepayments);
            decimal interest = ComputeAccruedInterest(l, lRepayments ?? []);
            return MapToDto(l, interest);
        }).ToList();
    }

    public async Task<LendingDto> GetLendingByIdAsync(string userId, string expenseBookId, string lendingId)
    {
        var lending = await GetOwnedLendingAsync(userId, expenseBookId, lendingId);
        var repayments = await _context.LendingRepayments
            .Find(r => r.LendingId == lendingId && !r.IsDeleted)
            .ToListAsync();

        decimal interest = ComputeAccruedInterest(lending, repayments);
        return MapToDto(lending, interest);
    }

    public async Task<LendingDto> CreateLendingAsync(string userId, CreateLendingRequest request)
    {
        await VerifyBookOwnershipAsync(userId, request.ExpenseBookId);

        var lending = new Lending
        {
            ExpenseBookId = request.ExpenseBookId,
            UserId = userId,
            BorrowerName = request.BorrowerName,
            BorrowerContact = request.BorrowerContact,
            PrincipalAmount = request.PrincipalAmount,
            AnnualInterestRate = request.AnnualInterestRate,
            StartDate = request.StartDate,
            DueDate = request.DueDate,
            OutstandingPrincipal = request.PrincipalAmount,
            Notes = request.Notes
        };

        await _context.Lendings.InsertOneAsync(lending);
        return MapToDto(lending, 0);
    }

    public async Task<LendingDto> UpdateLendingAsync(string userId, string expenseBookId, string lendingId, UpdateLendingRequest request)
    {
        var lending = await GetOwnedLendingAsync(userId, expenseBookId, lendingId);

        var update = Builders<Lending>.Update.Set(l => l.UpdatedAt, DateTime.UtcNow);

        if (request.BorrowerName is not null)
            update = update.Set(l => l.BorrowerName, request.BorrowerName);
        if (request.BorrowerContact is not null)
            update = update.Set(l => l.BorrowerContact, request.BorrowerContact);
        if (request.AnnualInterestRate.HasValue)
            update = update.Set(l => l.AnnualInterestRate, request.AnnualInterestRate.Value);
        if (request.DueDate.HasValue)
            update = update.Set(l => l.DueDate, request.DueDate.Value);
        if (request.Notes is not null)
            update = update.Set(l => l.Notes, request.Notes);

        await _context.Lendings.UpdateOneAsync(l => l.Id == lendingId, update);

        return await GetLendingByIdAsync(userId, expenseBookId, lendingId);
    }

    public async Task DeleteLendingAsync(string userId, string expenseBookId, string lendingId)
    {
        await GetOwnedLendingAsync(userId, expenseBookId, lendingId);

        var now = DateTime.UtcNow;
        await _context.Lendings.UpdateOneAsync(
            l => l.Id == lendingId,
            Builders<Lending>.Update
                .Set(l => l.IsDeleted, true)
                .Set(l => l.DeletedAt, now)
                .Set(l => l.UpdatedAt, now)
        );

        // Soft-delete all repayments
        await _context.LendingRepayments.UpdateManyAsync(
            r => r.LendingId == lendingId && !r.IsDeleted,
            Builders<LendingRepayment>.Update
                .Set(r => r.IsDeleted, true)
                .Set(r => r.DeletedAt, now)
                .Set(r => r.UpdatedAt, now)
        );
    }

    // ── Repayment CRUD ────────────────────────────────────────────────────────

    public async Task<LendingRepaymentsResponse> GetRepaymentsAsync(string userId, string expenseBookId, string lendingId, int page, int pageSize)
    {
        var lending = await GetOwnedLendingAsync(userId, expenseBookId, lendingId);

        var filter = Builders<LendingRepayment>.Filter.And(
            Builders<LendingRepayment>.Filter.Eq(r => r.LendingId, lendingId),
            Builders<LendingRepayment>.Filter.Eq(r => r.IsDeleted, false)
        );

        var total = (int)await _context.LendingRepayments.CountDocumentsAsync(filter);
        var items = await _context.LendingRepayments
            .Find(filter)
            .SortByDescending(r => r.Date)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        var allRepayments = await _context.LendingRepayments
            .Find(Builders<LendingRepayment>.Filter.And(
                Builders<LendingRepayment>.Filter.Eq(r => r.LendingId, lendingId),
                Builders<LendingRepayment>.Filter.Eq(r => r.IsDeleted, false)))
            .ToListAsync();

        decimal interest = ComputeAccruedInterest(lending, allRepayments);
        var lendingDto = MapToDto(lending, interest);

        return new LendingRepaymentsResponse
        {
            Lending = lendingDto,
            Items = items.Select(MapRepaymentToDto).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize,
            HasMore = page * pageSize < total
        };
    }

    public async Task<RepaymentDto> AddRepaymentAsync(string userId, string expenseBookId, string lendingId, CreateRepaymentRequest request)
    {
        var lending = await GetOwnedLendingAsync(userId, expenseBookId, lendingId);

        if (lending.Status == "settled")
            throw new InvalidOperationException("Cannot add repayment to a settled lending");

        if (request.Amount <= 0)
            throw new ArgumentException("Repayment amount must be positive");

        var repayment = new LendingRepayment
        {
            LendingId = lendingId,
            ExpenseBookId = expenseBookId,
            UserId = userId,
            Date = request.Date,
            Amount = request.Amount,
            Notes = request.Notes
        };

        await _context.LendingRepayments.InsertOneAsync(repayment);

        // Recalculate denormalized fields on the lending
        await RecalculateLendingSummaryAsync(lending);

        return MapRepaymentToDto(repayment);
    }

    public async Task DeleteRepaymentAsync(string userId, string expenseBookId, string lendingId, string repaymentId)
    {
        await GetOwnedLendingAsync(userId, expenseBookId, lendingId);

        var repayment = await _context.LendingRepayments
            .Find(r => r.Id == repaymentId && r.LendingId == lendingId && !r.IsDeleted)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Repayment not found");

        var now = DateTime.UtcNow;
        await _context.LendingRepayments.UpdateOneAsync(
            r => r.Id == repaymentId,
            Builders<LendingRepayment>.Update
                .Set(r => r.IsDeleted, true)
                .Set(r => r.DeletedAt, now)
                .Set(r => r.UpdatedAt, now)
        );

        // Recalculate after deletion
        var lending = await _context.Lendings
            .Find(l => l.Id == lendingId)
            .FirstOrDefaultAsync();

        if (lending is not null)
            await RecalculateLendingSummaryAsync(lending);
    }

    public async Task SettleLendingAsync(string userId, string expenseBookId, string lendingId, decimal? interestCollected, DateTime? settlementDate, string? notes)
    {
        var lending = await GetOwnedLendingAsync(userId, expenseBookId, lendingId);

        // Record the interest collection as a repayment before settling
        if (interestCollected.HasValue && interestCollected.Value > 0)
        {
            var repayment = new LendingRepayment
            {
                LendingId = lendingId,
                ExpenseBookId = expenseBookId,
                UserId = userId,
                Date = settlementDate ?? DateTime.UtcNow,
                Amount = interestCollected.Value,
                Notes = notes ?? "Interest settlement"
            };
            await _context.LendingRepayments.InsertOneAsync(repayment);

            // Update denormalized totals (interest payment doesn't reduce outstanding principal)
            await _context.Lendings.UpdateOneAsync(
                l => l.Id == lendingId,
                Builders<Lending>.Update
                    .Inc(l => l.TotalRepaid, interestCollected.Value)
                    .Inc(l => l.RepaymentCount, 1)
                    .Set(l => l.UpdatedAt, DateTime.UtcNow)
            );
        }

        await _context.Lendings.UpdateOneAsync(
            l => l.Id == lendingId,
            Builders<Lending>.Update
                .Set(l => l.Status, "settled")
                .Set(l => l.UpdatedAt, DateTime.UtcNow)
        );
    }

    private async Task RecalculateLendingSummaryAsync(Lending lending)
    {
        var repayments = await _context.LendingRepayments
            .Find(r => r.LendingId == lending.Id && !r.IsDeleted)
            .SortBy(r => r.Date)
            .ToListAsync();

        decimal totalRepaid = repayments.Sum(r => r.Amount);
        decimal outstanding = Math.Max(0, lending.PrincipalAmount - totalRepaid);

        // Auto-settle only when principal is fully repaid AND the loan carries no interest.
        // Interest-bearing loans must be manually settled (or when the user records the final interest payment).
        string status = (outstanding == 0 && lending.AnnualInterestRate == 0) ? "settled" : "active";

        await _context.Lendings.UpdateOneAsync(
            l => l.Id == lending.Id,
            Builders<Lending>.Update
                .Set(l => l.TotalRepaid, totalRepaid)
                .Set(l => l.OutstandingPrincipal, outstanding)
                .Set(l => l.RepaymentCount, repayments.Count)
                .Set(l => l.Status, status)
                .Set(l => l.UpdatedAt, DateTime.UtcNow)
        );
    }
}
