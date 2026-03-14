using ChinhNha.Application.DTOs.Admin;
using ChinhNha.Application.Interfaces;
using ChinhNha.Domain.Entities;
using ChinhNha.Domain.Interfaces;
using AutoMapper;

namespace ChinhNha.Application.Services;

public class AuditService : IAuditService
{
    private readonly IAuditRepository _auditRepository;
    private readonly IAppUserService _userService;
    private readonly IMapper _mapper;

    public AuditService(IAuditRepository auditRepository, IAppUserService userService, IMapper mapper)
    {
        _auditRepository = auditRepository;
        _userService = userService;
        _mapper = mapper;
    }

    public async Task LogActionAsync(string userId, string action, string entityType, int? entityId,
        string? oldValues = null, string? newValues = null, string? description = null,
        string? ipAddress = null, bool isSuccessful = true, string? errorMessage = null)
    {
        var auditLog = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            Description = description,
            IPAddress = ipAddress,
            IsSuccessful = isSuccessful,
            ErrorMessage = errorMessage,
            CreatedAt = DateTime.UtcNow
        };

        await _auditRepository.AddAsync(auditLog);
    }

    public async Task<AuditLogDto?> GetAuditLogByIdAsync(int id)
    {
        var log = await _auditRepository.GetByIdAsync(id);
        if (log == null) return null;

        var user = await _userService.GetUserByIdAsync(log.UserId);
        return MapToDto(log, user);
    }

    public async Task<IEnumerable<AuditLogDto>> GetAuditLogsAsync(int pageNumber = 1, int pageSize = 50)
    {
        var paginated = (await _auditRepository.GetAuditLogsAsync(pageNumber, pageSize)).ToList();
        var userMap = await BuildUserMapAsync(paginated);

        var result = new List<AuditLogDto>();
        foreach (var log in paginated)
        {
            userMap.TryGetValue(log.UserId, out var user);
            result.Add(MapToDto(log, user));
        }

        return result;
    }

    public async Task<IEnumerable<AuditLogDto>> GetAuditLogsByUserAsync(string userId, int pageNumber = 1, int pageSize = 50)
    {
        var logs = await _auditRepository.GetAuditLogsByUserIdAsync(userId, pageNumber, pageSize);
        var user = await _userService.GetUserByIdAsync(userId);

        return logs.Select(l => MapToDto(l, user));
    }

    public async Task<IEnumerable<AuditLogDto>> GetAuditLogsByDateRangeAsync(DateTime from, DateTime to, int pageNumber = 1, int pageSize = 50)
    {
        var logs = (await _auditRepository.GetAuditLogsByDateRangeAsync(from, to, pageNumber, pageSize)).ToList();
        var userMap = await BuildUserMapAsync(logs);
        var result = new List<AuditLogDto>();

        foreach (var log in logs)
        {
            userMap.TryGetValue(log.UserId, out var user);
            result.Add(MapToDto(log, user));
        }

        return result;
    }

    public async Task<IEnumerable<AuditLogDto>> GetAuditLogsByActionAsync(string action, int pageNumber = 1, int pageSize = 50)
    {
        var logs = (await _auditRepository.GetAuditLogsByActionAsync(action, pageNumber, pageSize)).ToList();
        var userMap = await BuildUserMapAsync(logs);
        var result = new List<AuditLogDto>();

        foreach (var log in logs)
        {
            userMap.TryGetValue(log.UserId, out var user);
            result.Add(MapToDto(log, user));
        }

        return result;
    }

    public async Task<IEnumerable<AuditLogDto>> GetAuditLogsByEntityAsync(string entityType, int? entityId = null, int pageNumber = 1, int pageSize = 50)
    {
        var logs = (await _auditRepository.GetAuditLogsByEntityAsync(entityType, entityId, pageNumber, pageSize)).ToList();
        var userMap = await BuildUserMapAsync(logs);
        var result = new List<AuditLogDto>();

        foreach (var log in logs)
        {
            userMap.TryGetValue(log.UserId, out var user);
            result.Add(MapToDto(log, user));
        }

        return result;
    }

    public async Task<AuditLogStatsDto> GetAuditLogStatsAsync()
    {
        var allLogs = await _auditRepository.ListAllAsync();

        var stats = new AuditLogStatsDto
        {
            TotalAuditLogs = allLogs.Count(),
            SuccessfulActions = allLogs.Count(l => l.IsSuccessful),
            FailedActions = allLogs.Count(l => !l.IsSuccessful),
            ActionCounts = allLogs
                .GroupBy(l => l.Action)
                .ToDictionary(g => g.Key, g => g.Count()),
            EntityTypeCounts = allLogs
                .GroupBy(l => l.EntityType)
                .ToDictionary(g => g.Key, g => g.Count()),
            UserActionCounts = allLogs
                .GroupBy(l => l.UserId)
                .ToDictionary(g => g.Key, g => g.Count())
        };

        return stats;
    }

    public async Task<int> GetTotalAuditLogsCountAsync()
    {
        return await _auditRepository.GetTotalAuditLogsCountAsync();
    }

    private AuditLogDto MapToDto(AuditLog log, AppUser? user)
    {
        return new AuditLogDto
        {
            Id = log.Id,
            UserId = log.UserId,
            UserName = user?.FullName ?? "Người dùng không xác định",
            UserEmail = user?.Email ?? string.Empty,
            Action = log.Action,
            EntityType = log.EntityType,
            EntityId = log.EntityId,
            OldValues = log.OldValues,
            NewValues = log.NewValues,
            IPAddress = log.IPAddress,
            UserAgent = log.UserAgent,
            Description = log.Description,
            CreatedAt = log.CreatedAt,
            IsSuccessful = log.IsSuccessful,
            ErrorMessage = log.ErrorMessage
        };
    }

    private async Task<Dictionary<string, AppUser?>> BuildUserMapAsync(IEnumerable<AuditLog> logs)
    {
        var map = new Dictionary<string, AppUser?>(StringComparer.Ordinal);
        foreach (var userId in logs.Select(l => l.UserId).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct())
        {
            map[userId] = await _userService.GetUserByIdAsync(userId);
        }

        return map;
    }
}
