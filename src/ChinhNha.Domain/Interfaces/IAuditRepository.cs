using ChinhNha.Domain.Entities;

namespace ChinhNha.Domain.Interfaces;

public interface IAuditRepository : IRepository<AuditLog>
{
    Task<IEnumerable<AuditLog>> GetAuditLogsAsync(int pageNumber = 1, int pageSize = 50);
    Task<IEnumerable<AuditLog>> GetAuditLogsByUserIdAsync(string userId, int pageNumber = 1, int pageSize = 50);
    Task<IEnumerable<AuditLog>> GetAuditLogsByDateRangeAsync(DateTime from, DateTime to, int pageNumber = 1, int pageSize = 50);
    Task<IEnumerable<AuditLog>> GetAuditLogsByActionAsync(string action, int pageNumber = 1, int pageSize = 50);
    Task<IEnumerable<AuditLog>> GetAuditLogsByEntityAsync(string entityType, int? entityId = null, int pageNumber = 1, int pageSize = 50);
    Task<int> GetTotalAuditLogsCountAsync();
    Task<int> GetFailedActionsCountAsync();
}
