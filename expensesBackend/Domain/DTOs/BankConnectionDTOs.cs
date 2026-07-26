namespace ExpensesBackend.API.Domain.DTOs;

public class CreateBankConnectionRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string? AccountMask { get; set; }
    public string Mode { get; set; } = "manual";
    public string AutoSyncSchedule { get; set; } = "on_demand";
}

public class UpdateBankConnectionRequest
{
    public string? DisplayName { get; set; }
    public string? Mode { get; set; }
    public string? AutoSyncSchedule { get; set; }
    public string? AccountMask { get; set; }
}

public class BankConnectionDto
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string? AccountMask { get; set; }
    public string Mode { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string AutoSyncSchedule { get; set; } = string.Empty;
    public DateTime? LastSyncedAt { get; set; }
    public bool IsConsentActive { get; set; }
    public DateTime? ConsentExpiry { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MapBankConnectionRequest
{
    // null = unmap the book from any bank connection
    public string? BankConnectionId { get; set; }
}

public class BookBankConnectionDto
{
    public string? BankConnectionId { get; set; }
    public BankConnectionDto? BankConnection { get; set; }
}
