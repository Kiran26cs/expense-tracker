namespace ExpensesBackend.API.Domain.DTOs;

public class AdminUserStatsDto
{
    public long Total { get; set; }
    public long NewThisMonth { get; set; }
    public long NewThisWeek { get; set; }
    public PlanBreakdownDto ByPlan { get; set; } = new();
    public List<MonthlyCountDto> GrowthByMonth { get; set; } = [];
}

public class PlanBreakdownDto
{
    public long Free { get; set; }
    public long Starter { get; set; }
    public long Pro { get; set; }
}

public class MonthlyCountDto
{
    public string Month { get; set; } = string.Empty; // "2026-01"
    public long Count { get; set; }
}

public class AdminSubscriptionStatsDto
{
    public long Active { get; set; }
    public long NewThisMonth { get; set; }
    public long CancelledThisMonth { get; set; }
    public long PendingCancellation { get; set; }
    public MrrBreakdownDto Mrr { get; set; } = new();
}

public class MrrBreakdownDto
{
    public long StarterCount { get; set; }
    public long ProCount { get; set; }
    public decimal StarterMrr { get; set; }
    public decimal ProMrr { get; set; }
    public decimal Total { get; set; }
}

public class AdminCreditStatsDto
{
    public long ConsumedThisMonth { get; set; }
    public CreditReasonBreakdownDto ByReason { get; set; } = new();
    public List<ZeroCreditBookDto> ZeroCreditBooks { get; set; } = [];
}

public class CreditReasonBreakdownDto
{
    public long AiChat { get; set; }
    public long AutoClassify { get; set; }
}

public class ZeroCreditBookDto
{
    public string BookId { get; set; } = string.Empty;
    public string BookName { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
}

public class AdminBookStatsDto
{
    public long Total { get; set; }
    public long TemplateBooks { get; set; }
    public long NewThisMonth { get; set; }
    public long AiChatEnabled { get; set; }
}

public class AdminImportStatsDto
{
    public ImportStatusBreakdownDto Last24h { get; set; } = new();
    public List<FailedImportSessionDto> FailedSessions { get; set; } = [];
}

public class ImportStatusBreakdownDto
{
    public long Completed { get; set; }
    public long Failed { get; set; }
    public long CompletedWithErrors { get; set; }
    public long Processing { get; set; }
    public long Queued { get; set; }
}

public class FailedImportSessionDto
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string BookId { get; set; } = string.Empty;
    public DateTime? FailedAt { get; set; }
}

public class AdminRecentActionsDto
{
    public List<AdminAuditLogDto> Actions { get; set; } = [];
}

public class AdminAuditLogDto
{
    public string Id { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string? TargetId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
