using ChinhNha.Application.DTOs.Admin;

namespace ChinhNha.Application.Interfaces;

public interface IAuditService
{
    Task LogActionAsync(string userId, string action, string entityType, int? entityId, 
        string? oldValues = null, string? newValues = null, string? description = null, 
        string? ipAddress = null, bool isSuccessful = true, string? errorMessage = null);
    
    Task<AuditLogDto?> GetAuditLogByIdAsync(int id);
    
    Task<IEnumerable<AuditLogDto>> GetAuditLogsAsync(int pageNumber = 1, int pageSize = 50);
    
    Task<IEnumerable<AuditLogDto>> GetAuditLogsByUserAsync(string userId, int pageNumber = 1, int pageSize = 50);
    
    Task<IEnumerable<AuditLogDto>> GetAuditLogsByDateRangeAsync(DateTime from, DateTime to, int pageNumber = 1, int pageSize = 50);
    
    Task<IEnumerable<AuditLogDto>> GetAuditLogsByActionAsync(string action, int pageNumber = 1, int pageSize = 50);
    
    Task<IEnumerable<AuditLogDto>> GetAuditLogsByEntityAsync(string entityType, int? entityId = null, int pageNumber = 1, int pageSize = 50);
    
    Task<AuditLogStatsDto> GetAuditLogStatsAsync();
    
    Task<int> GetTotalAuditLogsCountAsync();
}
