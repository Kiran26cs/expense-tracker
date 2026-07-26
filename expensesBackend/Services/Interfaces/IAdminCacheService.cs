namespace ExpensesBackend.API.Services.Interfaces;

public interface IAdminCacheService
{
    Task InvalidateUserAsync(string userId);
    Task InvalidateBookAsync(string bookId);
}
