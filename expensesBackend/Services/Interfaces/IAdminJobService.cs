using ExpensesBackend.API.Domain.DTOs;
using System.Threading.Channels;

namespace ExpensesBackend.API.Services.Interfaces;

public interface IAdminJobService
{
    Task<List<AdminImportSessionDto>> GetImportSessionsAsync(string? status);
    Task<AdminImportSessionDto?> GetSessionAsync(string sessionId);
    Task RetryImportAsync(string sessionId, Channel<ImportJobPayload> importChannel);
}
