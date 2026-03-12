using ChinhNha.Application.DTOs.Admin;
using ChinhNha.Application.Interfaces;
using ChinhNha.Domain.Entities;
using ChinhNha.Domain.Interfaces;
using ChinhNha.Infrastructure.Data;
using AutoMapper;

namespace ChinhNha.Infrastructure.Repositories;

public class AuditRepository : GenericRepository<AuditLog>, IAuditRepository
{
    public AuditRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<AuditLog>> GetAuditLogsByUserIdAsync(string userId, int pageNumber = 1, int pageSize = 50)
    {
        var allLogs = await ListAllAsync();
        return allLogs
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public async Task<IEnumerable<AuditLog>> GetAuditLogsByDateRangeAsync(DateTime from, DateTime to, int pageNumber = 1, int pageSize = 50)
    {
        var allLogs = await ListAllAsync();
        return allLogs
            .Where(l => l.CreatedAt >= from && l.CreatedAt <= to)
            .OrderByDescending(l => l.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public async Task<IEnumerable<AuditLog>> GetAuditLogsByActionAsync(string action, int pageNumber = 1, int pageSize = 50)
    {
        var allLogs = await ListAllAsync();
        return allLogs
            .Where(l => l.Action.Contains(action, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(l => l.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public async Task<IEnumerable<AuditLog>> GetAuditLogsByEntityAsync(string entityType, int? entityId = null, int pageNumber = 1, int pageSize = 50)
    {
        var allLogs = await ListAllAsync();
        var query = allLogs.Where(l => l.EntityType == entityType);
        
        if (entityId.HasValue)
            query = query.Where(l => l.EntityId == entityId);

        return query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public async Task<int> GetTotalAuditLogsCountAsync()
    {
        var allLogs = await ListAllAsync();
        return allLogs.Count();
    }

    public async Task<int> GetFailedActionsCountAsync()
    {
        var allLogs = await ListAllAsync();
        return allLogs.Count(l => !l.IsSuccessful);
    }
}
