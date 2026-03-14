using ChinhNha.Application.DTOs.Admin;
using ChinhNha.Application.Interfaces;
using ChinhNha.Domain.Entities;
using ChinhNha.Domain.Interfaces;
using ChinhNha.Infrastructure.Data;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace ChinhNha.Infrastructure.Repositories;

public class AuditRepository : GenericRepository<AuditLog>, IAuditRepository
{
    public AuditRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(int pageNumber = 1, int pageSize = 50)
    {
        return await _dbContext.Set<AuditLog>()
            .OrderByDescending(l => l.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetAuditLogsByUserIdAsync(string userId, int pageNumber = 1, int pageSize = 50)
    {
        return await _dbContext.Set<AuditLog>()
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetAuditLogsByDateRangeAsync(DateTime from, DateTime to, int pageNumber = 1, int pageSize = 50)
    {
        return await _dbContext.Set<AuditLog>()
            .Where(l => l.CreatedAt >= from && l.CreatedAt <= to)
            .OrderByDescending(l => l.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetAuditLogsByActionAsync(string action, int pageNumber = 1, int pageSize = 50)
    {
        action = action.Trim();
        return await _dbContext.Set<AuditLog>()
            .Where(l => l.Action.Contains(action))
            .OrderByDescending(l => l.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetAuditLogsByEntityAsync(string entityType, int? entityId = null, int pageNumber = 1, int pageSize = 50)
    {
        var query = _dbContext.Set<AuditLog>().Where(l => l.EntityType == entityType);
        
        if (entityId.HasValue)
            query = query.Where(l => l.EntityId == entityId);

        return await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalAuditLogsCountAsync()
    {
        return await _dbContext.Set<AuditLog>().CountAsync();
    }

    public async Task<int> GetFailedActionsCountAsync()
    {
        return await _dbContext.Set<AuditLog>().CountAsync(l => !l.IsSuccessful);
    }
}
