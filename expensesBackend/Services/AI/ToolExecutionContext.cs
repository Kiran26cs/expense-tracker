using ExpensesBackend.API.Domain.DTOs;

namespace ExpensesBackend.API.Services.AI;

public class ToolExecutionContext
{
    public string UserId { get; init; } = string.Empty;
    public string BookId { get; init; } = string.Empty;
    public ResolvedPermissions Permissions { get; init; } = new();
}
