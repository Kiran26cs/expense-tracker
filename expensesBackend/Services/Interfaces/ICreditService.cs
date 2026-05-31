using ExpensesBackend.API.Domain.DTOs;

namespace ExpensesBackend.API.Services.Interfaces;

public interface ICreditService
{
    /// <summary>Returns the credit balance for a book, creating the record if it doesn't exist.</summary>
    Task<CreditBalanceDto> GetBalanceAsync(string bookId);

    /// <summary>
    /// Checks if the book has credits available.
    /// Returns false (does not throw) so the caller can return a friendly 402.
    /// </summary>
    Task<bool> HasCreditsAsync(string bookId);

    /// <summary>Deducts 1 credit (paid first, then free) and records the transaction.</summary>
    Task DeductAsync(string bookId, string triggeredByUserId, List<string> toolsUsed);

    /// <summary>Admin-only: adds paid credits to a book.</summary>
    Task AdminGrantAsync(string bookId, int amount, string grantedByUserId);

    /// <summary>Resets free credits back to the limit for books whose lastResetDate is in a prior month.</summary>
    Task ResetMonthlyFreeCreditsAsync();

    /// <summary>
    /// Checks and consumes one Auto-classify usage.
    /// Within free quota  → free, increments counter.
    /// Quota exhausted but has credits → deducts 1 credit.
    /// Quota exhausted AND no credits → returns Allowed = false.
    /// </summary>
    Task<AutoClassifyConsumeResult> ConsumeAutoClassifyAsync(string bookId, string triggeredByUserId);
}
