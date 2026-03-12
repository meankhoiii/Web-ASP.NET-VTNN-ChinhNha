using ChinhNha.Domain.Entities;
using ChinhNha.Domain.Interfaces;
using ChinhNha.Infrastructure.Data;

namespace ChinhNha.Infrastructure.Repositories;

public class SavedSearchFilterRepository : GenericRepository<SavedSearchFilter>, ISavedSearchFilterRepository
{
    public SavedSearchFilterRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<SavedSearchFilter>> GetUserFiltersAsync(string userId)
    {
        var filters = await ListAllAsync();
        return filters
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.LastUsedAt ?? f.CreatedAt)
            .ToList();
    }

    public async Task<SavedSearchFilter?> GetUserFilterAsync(string userId, int filterId)
    {
        var filters = await ListAllAsync();
        return filters.FirstOrDefault(f => f.Id == filterId && f.UserId == userId);
    }

    public async Task<bool> DeleteUserFilterAsync(string userId, int filterId)
    {
        var filter = await GetUserFilterAsync(userId, filterId);
        if (filter == null) return false;
        
        await DeleteAsync(filter);
        return true;
    }
}
