using Azure.Identity;
using Azure.Storage.Blobs;
using ExpensesBackend.API.Domain.DTOs;
using ExpensesBackend.API.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System.Reflection;
using System.Text.Json;

namespace ExpensesBackend.API.Services;

public class TemplateBlobService : ITemplateBlobService
{
    private const string CacheKey       = "template:expense-book-v1";
    private const string EmbeddedName   = "ExpensesBackend.API.Templates.expense-book-template.json";

    private readonly IConfiguration _config;
    private readonly IMemoryCache   _cache;
    private readonly ILogger<TemplateBlobService> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TemplateBlobService(
        IConfiguration config,
        IMemoryCache cache,
        ILogger<TemplateBlobService> logger)
    {
        _config = config;
        _cache  = cache;
        _logger = logger;
    }

    public async Task<TemplateData> GetTemplateAsync()
    {
        if (_cache.TryGetValue(CacheKey, out TemplateData? cached))
            return cached!;

        TemplateData? template = null;

        var accountName = _config["AzureStorage:AccountName"];
        if (!string.IsNullOrEmpty(accountName))
        {
            try
            {
                template = await FetchFromBlobAsync(accountName);
                _logger.LogInformation("Template loaded from Azure Blob Storage");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch template from blob — falling back to embedded file");
            }
        }

        template ??= LoadEmbedded();

        _cache.Set(CacheKey, template, TimeSpan.FromHours(1));
        return template;
    }

    private async Task<TemplateData> FetchFromBlobAsync(string accountName)
    {
        var container  = _config["AzureStorage:TemplateContainer"]  ?? "templates";
        var blobName   = _config["AzureStorage:TemplateBlobName"]   ?? "expense-book-template.json";

        var blobClient = new BlobClient(
            new Uri($"https://{accountName}.blob.core.windows.net/{container}/{blobName}"),
            new DefaultAzureCredential());

        var response = await blobClient.DownloadContentAsync();
        var json     = response.Value.Content.ToString();

        return JsonSerializer.Deserialize<TemplateData>(json, JsonOpts)
            ?? throw new InvalidDataException("Blob template JSON deserialised to null");
    }

    private static TemplateData LoadEmbedded()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(EmbeddedName)
            ?? throw new FileNotFoundException($"Embedded template resource '{EmbeddedName}' not found");
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return JsonSerializer.Deserialize<TemplateData>(json, JsonOpts)
            ?? throw new InvalidDataException("Embedded template JSON deserialised to null");
    }
}
