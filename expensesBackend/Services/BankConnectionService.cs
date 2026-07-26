using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Domain.Entities;
using ExpensesBackend.API.Services.Interfaces;
using MongoDB.Driver;

namespace ExpensesBackend.API.Services;

public class BankConnectionService : IBankConnectionService
{
    private static readonly HashSet<string> ValidBanks = new(StringComparer.OrdinalIgnoreCase)
    {
        "HDFC", "ICICI", "SBI", "Axis", "Kotak", "IndusInd", "Other"
    };

    private static readonly HashSet<string> ValidModes = new(StringComparer.OrdinalIgnoreCase)
    {
        "manual", "auto", "disabled"
    };

    private static readonly HashSet<string> ValidSchedules = new(StringComparer.OrdinalIgnoreCase)
    {
        "daily", "weekly", "on_demand"
    };

    private readonly MongoDbContext _context;

    public BankConnectionService(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<List<BankConnectionDto>> GetConnectionsAsync(string userId)
    {
        var filter = Builders<BankConnection>.Filter.And(
            Builders<BankConnection>.Filter.Eq(c => c.UserId, userId),
            Builders<BankConnection>.Filter.Eq(c => c.IsActive, true));

        var connections = await _context.BankConnections
            .Find(filter)
            .SortByDescending(c => c.CreatedAt)
            .ToListAsync();

        return connections.Select(MapToDto).ToList();
    }

    public async Task<BankConnectionDto> GetConnectionAsync(string connectionId, string userId)
    {
        var connection = await LoadAsync(connectionId, userId)
            ?? throw new KeyNotFoundException("Bank connection not found");

        return MapToDto(connection);
    }

    public async Task<BankConnectionDto> CreateConnectionAsync(string userId, CreateBankConnectionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName))
            throw new ArgumentException("Display name is required");

        if (!ValidBanks.Contains(request.BankName))
            throw new ArgumentException($"Unsupported bank: '{request.BankName}'. Supported: HDFC, ICICI, SBI, Axis, Kotak, Other");

        if (!ValidModes.Contains(request.Mode))
            throw new ArgumentException($"Invalid mode: '{request.Mode}'. Use: manual, auto, disabled");

        if (!ValidSchedules.Contains(request.AutoSyncSchedule))
            throw new ArgumentException($"Invalid schedule: '{request.AutoSyncSchedule}'. Use: daily, weekly, on_demand");

        // auto mode is pending Setu signup — block it for now
        if (request.Mode.Equals("auto", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Automatic sync via Account Aggregator is not yet available. Use manual mode.");

        var connection = new BankConnection
        {
            UserId           = userId,
            DisplayName      = request.DisplayName.Trim(),
            BankName         = request.BankName.Trim(),
            AccountMask      = request.AccountMask?.Trim(),
            Mode             = request.Mode.ToLowerInvariant(),
            Provider         = "manual",
            AutoSyncSchedule = request.AutoSyncSchedule.ToLowerInvariant(),
            IsActive         = true,
            CreatedAt        = DateTime.UtcNow,
            UpdatedAt        = DateTime.UtcNow
        };

        await _context.BankConnections.InsertOneAsync(connection);
        return MapToDto(connection);
    }

    public async Task<BankConnectionDto> UpdateConnectionAsync(
        string connectionId, string userId, UpdateBankConnectionRequest request)
    {
        var connection = await LoadAsync(connectionId, userId)
            ?? throw new KeyNotFoundException("Bank connection not found");

        if (request.DisplayName != null)
            connection.DisplayName = request.DisplayName.Trim();

        if (request.AccountMask != null)
            connection.AccountMask = request.AccountMask.Trim();

        if (request.Mode != null)
        {
            if (!ValidModes.Contains(request.Mode))
                throw new ArgumentException($"Invalid mode: '{request.Mode}'");

            if (request.Mode.Equals("auto", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Automatic sync via Account Aggregator is not yet available.");

            connection.Mode = request.Mode.ToLowerInvariant();
        }

        if (request.AutoSyncSchedule != null)
        {
            if (!ValidSchedules.Contains(request.AutoSyncSchedule))
                throw new ArgumentException($"Invalid schedule: '{request.AutoSyncSchedule}'");

            connection.AutoSyncSchedule = request.AutoSyncSchedule.ToLowerInvariant();
        }

        connection.UpdatedAt = DateTime.UtcNow;

        var filter = Builders<BankConnection>.Filter.Eq(c => c.Id, connectionId);
        await _context.BankConnections.ReplaceOneAsync(filter, connection);

        return MapToDto(connection);
    }

    public async Task DeleteConnectionAsync(string connectionId, string userId)
    {
        var connection = await LoadAsync(connectionId, userId)
            ?? throw new KeyNotFoundException("Bank connection not found");

        // Unmap from any books that reference this connection
        var bookFilter = Builders<ExpenseBook>.Filter.And(
            Builders<ExpenseBook>.Filter.Eq(b => b.UserId, userId),
            Builders<ExpenseBook>.Filter.Eq(b => b.BankConnectionId, connectionId));

        var bookUpdate = Builders<ExpenseBook>.Update
            .Unset(b => b.BankConnectionId)
            .Set(b => b.UpdatedAt, DateTime.UtcNow);

        await _context.ExpenseBooks.UpdateManyAsync(bookFilter, bookUpdate);

        // Soft-delete the connection
        var filter = Builders<BankConnection>.Filter.Eq(c => c.Id, connectionId);
        var update = Builders<BankConnection>.Update
            .Set(c => c.IsActive, false)
            .Set(c => c.UpdatedAt, DateTime.UtcNow);

        await _context.BankConnections.UpdateOneAsync(filter, update);
    }

    public async Task<BookBankConnectionDto> GetBookConnectionAsync(string expenseBookId, string userId)
    {
        var book = await LoadBookAsync(expenseBookId, userId)
            ?? throw new KeyNotFoundException("Expense book not found");

        if (string.IsNullOrEmpty(book.BankConnectionId))
            return new BookBankConnectionDto();

        var connection = await LoadAsync(book.BankConnectionId, userId);
        return new BookBankConnectionDto
        {
            BankConnectionId = book.BankConnectionId,
            BankConnection   = connection != null ? MapToDto(connection) : null
        };
    }

    public async Task MapToBookAsync(string expenseBookId, string userId, string? bankConnectionId)
    {
        var book = await LoadBookAsync(expenseBookId, userId)
            ?? throw new KeyNotFoundException("Expense book not found");

        if (!string.IsNullOrEmpty(bankConnectionId))
        {
            var connection = await LoadAsync(bankConnectionId, userId)
                ?? throw new KeyNotFoundException("Bank connection not found");

            _ = connection; // verified it exists and belongs to user
        }

        var filter = Builders<ExpenseBook>.Filter.Eq(b => b.Id, expenseBookId);

        UpdateDefinition<ExpenseBook> update;
        if (string.IsNullOrEmpty(bankConnectionId))
        {
            update = Builders<ExpenseBook>.Update
                .Unset(b => b.BankConnectionId)
                .Set(b => b.UpdatedAt, DateTime.UtcNow);
        }
        else
        {
            update = Builders<ExpenseBook>.Update
                .Set(b => b.BankConnectionId, bankConnectionId)
                .Set(b => b.UpdatedAt, DateTime.UtcNow);
        }

        await _context.ExpenseBooks.UpdateOneAsync(filter, update);
    }

    // ── Private helpers ─────────────────────────────────────────────────────────

    private async Task<BankConnection?> LoadAsync(string connectionId, string userId)
    {
        var filter = Builders<BankConnection>.Filter.And(
            Builders<BankConnection>.Filter.Eq(c => c.Id, connectionId),
            Builders<BankConnection>.Filter.Eq(c => c.UserId, userId),
            Builders<BankConnection>.Filter.Eq(c => c.IsActive, true));

        return await _context.BankConnections.Find(filter).FirstOrDefaultAsync();
    }

    private async Task<ExpenseBook?> LoadBookAsync(string expenseBookId, string userId)
    {
        var filter = Builders<ExpenseBook>.Filter.And(
            Builders<ExpenseBook>.Filter.Eq(b => b.Id, expenseBookId),
            Builders<ExpenseBook>.Filter.Eq(b => b.UserId, userId));

        return await _context.ExpenseBooks.Find(filter).FirstOrDefaultAsync();
    }

    private static BankConnectionDto MapToDto(BankConnection c) => new()
    {
        Id               = c.Id,
        DisplayName      = c.DisplayName,
        BankName         = c.BankName,
        AccountMask      = c.AccountMask,
        Mode             = c.Mode,
        Provider         = c.Provider,
        AutoSyncSchedule = c.AutoSyncSchedule,
        LastSyncedAt     = c.LastSyncedAt,
        IsConsentActive  = c.ConsentHandle != null && c.ConsentExpiry > DateTime.UtcNow,
        ConsentExpiry    = c.ConsentExpiry,
        IsActive         = c.IsActive,
        CreatedAt        = c.CreatedAt
    };
}
