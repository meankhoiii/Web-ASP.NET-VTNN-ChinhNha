using ChinhNha.Application.DTOs.Admin;
using ChinhNha.Application.Interfaces;
using ChinhNha.Domain.Entities;
using ChinhNha.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace ChinhNha.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _context;
    private readonly IPasswordHashService _passwordHashService;

    public UserService(AppDbContext context, IPasswordHashService passwordHashService)
    {
        _context = context;
        _passwordHashService = passwordHashService;
    }

    public async Task<UserPagedResultDto> GetPagedUsersAsync(UserFilterDto filter, int page, int pageSize)
    {
        var baseQuery = _context.Users.AsNoTracking();
        var filtered = ApplyFilter(baseQuery, filter);

        var totalCount = await baseQuery.CountAsync();
        var filteredCount = await filtered.CountAsync();

        var sorted = ApplySort(filtered, filter);
        var users = await sorted
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var userIds = users.Select(u => u.Id).ToList();
        var usersWithOrders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.UserId != null && userIds.Contains(o.UserId))
            .Select(o => o.UserId!)
            .Distinct()
            .ToListAsync();

        var hasOrdersSet = usersWithOrders.ToHashSet(StringComparer.Ordinal);

        var items = users.Select(u =>
        {
            var canDelete = string.Equals(u.Role, "Customer", StringComparison.OrdinalIgnoreCase)
                && !hasOrdersSet.Contains(u.Id);

            string? cannotDeleteReason = null;
            if (!canDelete)
            {
                if (!string.Equals(u.Role, "Customer", StringComparison.OrdinalIgnoreCase))
                {
                    cannotDeleteReason = "Chỉ có thể xóa tài khoản Customer.";
                }
                else if (hasOrdersSet.Contains(u.Id))
                {
                    cannotDeleteReason = "Không thể xóa user đã có đơn hàng.";
                }
            }

            return new UserListItemDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Phone = u.Phone,
                Role = u.Role,
                IsActive = u.IsActive,
                AvatarUrl = u.AvatarUrl,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt,
                CanDelete = canDelete,
                CannotDeleteReason = cannotDeleteReason
            };
        }).ToList();

        return new UserPagedResultDto
        {
            Items = items,
            TotalCount = totalCount,
            FilteredCount = filteredCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<UserStatsDto> GetUserStatsAsync(UserFilterDto? filter = null)
    {
        var query = _context.Users.AsNoTracking();
        if (filter != null)
        {
            query = ApplyFilter(query, filter);
        }

        var users = await query.ToListAsync();

        return new UserStatsDto
        {
            TotalUsers = users.Count,
            ActiveUsers = users.Count(x => x.IsActive),
            InactiveUsers = users.Count(x => !x.IsActive),
            AdminUsers = users.Count(x => string.Equals(x.Role, "Admin", StringComparison.OrdinalIgnoreCase)),
            StaffUsers = users.Count(x => string.Equals(x.Role, "Staff", StringComparison.OrdinalIgnoreCase)),
            CustomerUsers = users.Count(x => string.Equals(x.Role, "Customer", StringComparison.OrdinalIgnoreCase))
        };
    }

    public async Task<UserDetailDto?> GetUserDetailAsync(string id)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (user == null)
        {
            return null;
        }

        var orders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == id)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return new UserDetailDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            Role = user.Role,
            IsActive = user.IsActive,
            AvatarUrl = user.AvatarUrl,
            DateOfBirth = user.DateOfBirth,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            TotalOrders = orders.Count,
            TotalSpent = orders.Sum(x => x.TotalAmount),
            LastOrderDate = orders.FirstOrDefault()?.OrderDate,
            RecentOrders = orders.Take(5)
                .Select(o => new UserDetailOrderDto
                {
                    OrderId = o.Id,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status.ToString()
                })
                .ToList()
        };
    }

    public Task<UserDetailDto?> GetUserForEditAsync(string id)
    {
        return GetUserDetailAsync(id);
    }

    public async Task<(bool Success, string Message, string? UserId)> CreateUserAsync(CreateUserRequestDto request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var exists = await _context.Users.AnyAsync(x => x.Email.ToLower() == normalizedEmail);
        if (exists)
        {
            return (false, "Email đã tồn tại.", null);
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            PasswordHash = _passwordHashService.HashPassword(request.Password),
            Role = NormalizeRole(request.Role),
            IsActive = request.IsActive,
            DateOfBirth = request.DateOfBirth,
            AvatarUrl = request.AvatarUrl,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return (true, "Tạo người dùng thành công.", user.Id);
    }

    public async Task<(bool Success, string Message)> UpdateUserAsync(string id, UpdateUserRequestDto request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user == null)
        {
            return (false, "Không tìm thấy người dùng.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var duplicated = await _context.Users.AnyAsync(x => x.Id != id && x.Email.ToLower() == normalizedEmail);
        if (duplicated)
        {
            return (false, "Email đã tồn tại.");
        }

        user.FullName = request.FullName.Trim();
        user.Email = normalizedEmail;
        user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        user.Role = NormalizeRole(request.Role);
        user.IsActive = request.IsActive;
        user.DateOfBirth = request.DateOfBirth;
        user.AvatarUrl = request.AvatarUrl;

        await _context.SaveChangesAsync();
        return (true, "Cập nhật người dùng thành công.");
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(string id, string newPassword)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user == null)
        {
            return (false, "Không tìm thấy người dùng.");
        }

        user.PasswordHash = _passwordHashService.HashPassword(newPassword);
        await _context.SaveChangesAsync();

        return (true, "Đổi mật khẩu thành công.");
    }

    public async Task<(bool Success, string Message, bool CurrentState)> ToggleActiveAsync(string id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user == null)
        {
            return (false, "Không tìm thấy người dùng.", false);
        }

        user.IsActive = !user.IsActive;
        await _context.SaveChangesAsync();

        return (true, "Cập nhật trạng thái thành công.", user.IsActive);
    }

    public async Task<(bool Success, string Message)> SetActiveBulkAsync(IEnumerable<string> ids, bool isActive)
    {
        var idList = ids.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        if (!idList.Any())
        {
            return (false, "Chưa chọn người dùng nào.");
        }

        var users = await _context.Users.Where(x => idList.Contains(x.Id)).ToListAsync();
        foreach (var user in users)
        {
            user.IsActive = isActive;
        }

        await _context.SaveChangesAsync();
        return (true, isActive ? "Đã kích hoạt các tài khoản đã chọn." : "Đã vô hiệu hóa các tài khoản đã chọn.");
    }

    public async Task<(bool Success, string Message)> DeleteUserAsync(string id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user == null)
        {
            return (false, "Không tìm thấy người dùng.");
        }

        if (!string.Equals(user.Role, "Customer", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Chỉ cho phép xóa tài khoản Customer.");
        }

        var hasOrder = await _context.Orders.AnyAsync(x => x.UserId == id);
        if (hasOrder)
        {
            return (false, "Không thể xóa user đã có đơn hàng.");
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return (true, "Xóa người dùng thành công.");
    }

    public async Task<byte[]> ExportUsersExcelAsync(UserFilterDto filter, IEnumerable<string>? selectedIds = null)
    {
        var query = ApplyFilter(_context.Users.AsNoTracking(), filter);

        var selected = selectedIds?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        if (selected != null && selected.Any())
        {
            query = query.Where(x => selected.Contains(x.Id));
        }

        var users = await ApplySort(query, filter).ToListAsync();

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Users");

        ws.Cells[1, 1].Value = "Họ tên";
        ws.Cells[1, 2].Value = "Email";
        ws.Cells[1, 3].Value = "Số điện thoại";
        ws.Cells[1, 4].Value = "Vai trò";
        ws.Cells[1, 5].Value = "Trạng thái";
        ws.Cells[1, 6].Value = "Lần đăng nhập cuối";
        ws.Cells[1, 7].Value = "Ngày tạo";

        for (var i = 0; i < users.Count; i++)
        {
            var row = i + 2;
            var u = users[i];
            ws.Cells[row, 1].Value = u.FullName;
            ws.Cells[row, 2].Value = u.Email;
            ws.Cells[row, 3].Value = u.Phone;
            ws.Cells[row, 4].Value = u.Role;
            ws.Cells[row, 5].Value = u.IsActive ? "Active" : "Inactive";
            ws.Cells[row, 6].Value = u.LastLoginAt?.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            ws.Cells[row, 7].Value = u.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
        }

        ws.Cells[ws.Dimension.Address].AutoFitColumns();
        return await Task.FromResult(package.GetAsByteArray());
    }

    private static IQueryable<AppUser> ApplyFilter(IQueryable<AppUser> query, UserFilterDto filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var search = filter.SearchTerm.Trim().ToLower();
            query = query.Where(x => x.FullName.ToLower().Contains(search)
                || x.Email.ToLower().Contains(search)
                || (x.Phone != null && x.Phone.ToLower().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Role))
        {
            var role = filter.Role.Trim().ToLower();
            query = query.Where(x => x.Role.ToLower() == role);
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == filter.IsActive.Value);
        }

        if (filter.CreatedFrom.HasValue)
        {
            var from = filter.CreatedFrom.Value.Date;
            query = query.Where(x => x.CreatedAt >= from);
        }

        if (filter.CreatedTo.HasValue)
        {
            var to = filter.CreatedTo.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(x => x.CreatedAt <= to);
        }

        return query;
    }

    private static IQueryable<AppUser> ApplySort(IQueryable<AppUser> query, UserFilterDto filter)
    {
        var sortBy = (filter.SortBy ?? "CreatedAt").Trim();
        var desc = string.Equals(filter.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return sortBy switch
        {
            "FullName" => desc ? query.OrderByDescending(x => x.FullName) : query.OrderBy(x => x.FullName),
            "Email" => desc ? query.OrderByDescending(x => x.Email) : query.OrderBy(x => x.Email),
            "Phone" => desc ? query.OrderByDescending(x => x.Phone) : query.OrderBy(x => x.Phone),
            "Role" => desc ? query.OrderByDescending(x => x.Role) : query.OrderBy(x => x.Role),
            "IsActive" => desc ? query.OrderByDescending(x => x.IsActive) : query.OrderBy(x => x.IsActive),
            "LastLoginAt" => desc ? query.OrderByDescending(x => x.LastLoginAt) : query.OrderBy(x => x.LastLoginAt),
            _ => desc ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt)
        };
    }

    private static string NormalizeRole(string role)
    {
        return role.Trim().ToLower() switch
        {
            "admin" => "Admin",
            "staff" => "Staff",
            _ => "Customer"
        };
    }
}
