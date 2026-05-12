using ExpensesBackend.API.Domain.DTOs;

namespace ExpensesBackend.API.Services.Interfaces;

public interface ITemplateBookService
{
    Task<StartTemplateResult> StartTemplateCreationAsync(string userId, string currency);
}

public class StartTemplateResult
{
    public bool AlreadyExists { get; set; }
    public string? BookId { get; set; }
    public string? SessionId { get; set; }
}
