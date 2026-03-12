namespace ChinhNha.Application.DTOs.Admin;

public class AuditLogDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    
    public string? IPAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Description { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
}

public class CreateAuditLogDto
{
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? Description { get; set; }
    public bool IsSuccessful { get; set; } = true;
    public string? ErrorMessage { get; set; }
}

public class AuditLogFilterDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? UserId { get; set; }
    public string? Action { get; set; }
    public string? EntityType { get; set; }
    public bool? IsSuccessful { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class AuditLogStatsDto
{
    public int TotalAuditLogs { get; set; }
    public int SuccessfulActions { get; set; }
    public int FailedActions { get; set; }
    public Dictionary<string, int> ActionCounts { get; set; } = new();
    public Dictionary<string, int> EntityTypeCounts { get; set; } = new();
    public Dictionary<string, int> UserActionCounts { get; set; } = new();
}
