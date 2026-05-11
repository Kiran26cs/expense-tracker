using ExpensesBackend.API.Domain.DTOs;

namespace ExpensesBackend.API.Services.Interfaces;

public interface ITemplateBlobService
{
    Task<TemplateData> GetTemplateAsync();
}
