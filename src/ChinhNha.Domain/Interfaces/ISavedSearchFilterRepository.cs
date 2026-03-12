using ChinhNha.Domain.Entities;

namespace ChinhNha.Domain.Interfaces;

public interface ISavedSearchFilterRepository : IRepository<SavedSearchFilter>
{
    Task<IEnumerable<SavedSearchFilter>> GetUserFiltersAsync(string userId);
    Task<SavedSearchFilter?> GetUserFilterAsync(string userId, int filterId);
    Task<bool> DeleteUserFilterAsync(string userId, int filterId);
}
