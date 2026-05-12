using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Domain.Entities;
using ExpensesBackend.API.Infrastructure.Data;
using ExpensesBackend.API.Services.Interfaces;
using MongoDB.Driver;
using System.Threading.Channels;

namespace ExpensesBackend.API.Services;

public class TemplateBookService : ITemplateBookService
{
    private readonly MongoDbContext _context;
    private readonly Channel<TemplateCreationJobPayload> _channel;

    public TemplateBookService(
        MongoDbContext context,
        Channel<TemplateCreationJobPayload> channel)
    {
        _context = context;
        _channel = channel;
    }

    public async Task<StartTemplateResult> StartTemplateCreationAsync(string userId, string currency)
    {
        // One-per-user guard
        var existing = await _context.ExpenseBooks
            .Find(Builders<ExpenseBook>.Filter.And(
                Builders<ExpenseBook>.Filter.Eq(b => b.UserId, userId),
                Builders<ExpenseBook>.Filter.Eq(b => b.IsTemplate, true)))
            .FirstOrDefaultAsync();

        if (existing != null)
            return new StartTemplateResult { AlreadyExists = true };

        // Determine if this becomes the default book
        var hasBooks = await _context.ExpenseBooks
            .Find(Builders<ExpenseBook>.Filter.Eq(b => b.UserId, userId))
            .AnyAsync();

        var book = new ExpenseBook
        {
            UserId      = userId,
            Name        = "Demo Expense Book",
            Category    = "Personal",
            Currency    = string.IsNullOrWhiteSpace(currency) ? "USD" : currency,
            Icon        = "fa-solid fa-wallet",
            Color       = "#6366f1",
            IsTemplate  = true,
            IsDefault   = !hasBooks
        };
        await _context.ExpenseBooks.InsertOneAsync(book);

        // Build 5-phase ImportSession for progress tracking
        var session = new ImportSession
        {
            ExpenseBookId = book.Id,
            UserId        = userId,
            FileName      = "Demo Expense Book",
            JobType       = "templateCreation",
            Status        = ImportStatus.Queued,
            TotalRecords  = 5,
            Records       =
            [
                new() { RowNumber = 1, Description = "Categories",        Amount = 10, Category = "template" },
                new() { RowNumber = 2, Description = "Expenses",          Amount = 57, Category = "template" },
                new() { RowNumber = 3, Description = "Budgets",           Amount = 5,  Category = "template" },
                new() { RowNumber = 4, Description = "Upcoming Payments", Amount = 4,  Category = "template" },
                new() { RowNumber = 5, Description = "Lendings",          Amount = 2,  Category = "template" }
            ]
        };
        await _context.ImportSessions.InsertOneAsync(session);

        await _channel.Writer.WriteAsync(new TemplateCreationJobPayload
        {
            ImportSessionId = session.Id,
            ExpenseBookId   = book.Id,
            UserId          = userId,
            Currency        = book.Currency
        });

        return new StartTemplateResult
        {
            AlreadyExists = false,
            BookId        = book.Id,
            SessionId     = session.Id
        };
    }
}
