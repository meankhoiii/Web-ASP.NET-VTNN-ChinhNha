using ChinhNha.Application.DTOs.Admin;

namespace ChinhNha.Application.Interfaces;

public interface IUserService
{
    Task<UserPagedResultDto> GetPagedUsersAsync(UserFilterDto filter, int page, int pageSize);
    Task<UserStatsDto> GetUserStatsAsync(UserFilterDto? filter = null);
    Task<UserDetailDto?> GetUserDetailAsync(string id);
    Task<UserDetailDto?> GetUserForEditAsync(string id);
    Task<(bool Success, string Message, string? UserId)> CreateUserAsync(CreateUserRequestDto request);
    Task<(bool Success, string Message)> UpdateUserAsync(string id, UpdateUserRequestDto request);
    Task<(bool Success, string Message)> ChangePasswordAsync(string id, string newPassword);
    Task<(bool Success, string Message, bool CurrentState)> ToggleActiveAsync(string id);
    Task<(bool Success, string Message)> SetActiveBulkAsync(IEnumerable<string> ids, bool isActive);
    Task<(bool Success, string Message)> DeleteUserAsync(string id);
    Task<byte[]> ExportUsersExcelAsync(UserFilterDto filter, IEnumerable<string>? selectedIds = null);
}
