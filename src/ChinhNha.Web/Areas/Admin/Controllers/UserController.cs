using System.Security.Claims;
using System.Text.RegularExpressions;
using ChinhNha.Application.DTOs.Admin;
using ChinhNha.Application.Interfaces;
using ChinhNha.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChinhNha.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class UserController : Controller
{
    private static readonly HashSet<string> AllowedImageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    private readonly IUserService _userService;
    private readonly IAuditService _auditService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<UserController> _logger;

    public UserController(
        IUserService userService,
        IAuditService auditService,
        IWebHostEnvironment environment,
        ILogger<UserController> logger)
    {
        _userService = userService;
        _auditService = auditService;
        _environment = environment;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] UserFilterViewModel filter, int page = 1, int pageSize = 10)
    {
        if (Request.Query.ContainsKey("draw"))
        {
            var draw = ParseInt(Request.Query["draw"], 1);
            var start = ParseInt(Request.Query["start"], 0);
            var length = Math.Clamp(ParseInt(Request.Query["length"], pageSize), 10, 100);
            page = (start / length) + 1;
            pageSize = length;

            var searchValue = Request.Query["search[value]"].ToString();
            var orderColumnIndex = Request.Query["order[0][column]"].ToString();
            var orderDir = Request.Query["order[0][dir]"].ToString();
            var sortBy = Request.Query[$"columns[{orderColumnIndex}][data]"].ToString();

            var dtoFilter = BuildFilterDto(filter, searchValue, sortBy, orderDir);
            var paged = await _userService.GetPagedUsersAsync(dtoFilter, page, pageSize);

            return Json(new
            {
                draw,
                recordsTotal = paged.TotalCount,
                recordsFiltered = paged.FilteredCount,
                data = paged.Items.Select(x => new
                {
                    x.Id,
                    x.FullName,
                    x.Email,
                    x.Phone,
                    x.Role,
                    x.IsActive,
                    x.AvatarUrl,
                    createdAt = x.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                    lastLoginAt = x.LastLoginAt?.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                    x.CanDelete,
                    x.CannotDeleteReason
                })
            });
        }

        var stats = await _userService.GetUserStatsAsync(BuildFilterDto(filter));
        var vm = new UserListViewModel
        {
            Filter = filter,
            Stats = stats
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserFormViewModel form)
    {
        if (!ValidatePhone(form.Phone))
        {
            return Json(new { success = false, message = "Số điện thoại không đúng định dạng Việt Nam." });
        }

        if (string.IsNullOrWhiteSpace(form.Password) || form.Password.Length < 8)
        {
            return Json(new { success = false, message = "Mật khẩu phải tối thiểu 8 ký tự." });
        }

        if (!string.Equals(form.Password, form.ConfirmPassword, StringComparison.Ordinal))
        {
            return Json(new { success = false, message = "Xác nhận mật khẩu không khớp." });
        }

        var avatarUrl = await SaveAvatarAsync(form.AvatarFile, form.AvatarUrl);
        if (avatarUrl == null && form.AvatarFile != null)
        {
            return Json(new { success = false, message = "Avatar không hợp lệ. Chỉ chấp nhận JPG/JPEG/PNG/WEBP/GIF." });
        }

        var result = await _userService.CreateUserAsync(new CreateUserRequestDto
        {
            FullName = form.FullName,
            Email = form.Email,
            Phone = form.Phone,
            Password = form.Password,
            Role = form.Role,
            IsActive = form.IsActive,
            DateOfBirth = form.DateOfBirth,
            AvatarUrl = avatarUrl ?? form.AvatarUrl
        });

        await WriteAuditLogAsync("CreateUser", $"Tạo user {form.Email}", result.Success, result.Message);

        return Json(new
        {
            success = result.Success,
            message = result.Message,
            data = result.Success ? new { id = result.UserId } : null
        });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var data = await _userService.GetUserForEditAsync(id);
        if (data == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy người dùng." });
        }

        return Json(new
        {
            success = true,
            data = new
            {
                data.Id,
                data.FullName,
                data.Email,
                data.Phone,
                data.Role,
                data.IsActive,
                dateOfBirth = data.DateOfBirth?.ToString("yyyy-MM-dd"),
                data.AvatarUrl
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, UserFormViewModel form)
    {
        if (!ValidatePhone(form.Phone))
        {
            return Json(new { success = false, message = "Số điện thoại không đúng định dạng Việt Nam." });
        }

        var avatarUrl = await SaveAvatarAsync(form.AvatarFile, form.AvatarUrl);
        if (avatarUrl == null && form.AvatarFile != null)
        {
            return Json(new { success = false, message = "Avatar không hợp lệ. Chỉ chấp nhận JPG/JPEG/PNG/WEBP/GIF." });
        }

        var result = await _userService.UpdateUserAsync(id, new UpdateUserRequestDto
        {
            FullName = form.FullName,
            Email = form.Email,
            Phone = form.Phone,
            Role = form.Role,
            IsActive = form.IsActive,
            DateOfBirth = form.DateOfBirth,
            AvatarUrl = avatarUrl ?? form.AvatarUrl
        });

        await WriteAuditLogAsync("EditUser", $"Sửa user {id}", result.Success, result.Message);
        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(string id, string newPassword, string confirmNewPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
        {
            return Json(new { success = false, message = "Mật khẩu phải tối thiểu 8 ký tự." });
        }

        if (!string.Equals(newPassword, confirmNewPassword, StringComparison.Ordinal))
        {
            return Json(new { success = false, message = "Xác nhận mật khẩu không khớp." });
        }

        var result = await _userService.ChangePasswordAsync(id, newPassword);
        await WriteAuditLogAsync("ChangeUserPassword", $"Đổi mật khẩu user {id}", result.Success, result.Message);

        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpGet]
    public async Task<IActionResult> Detail(string id)
    {
        var detail = await _userService.GetUserDetailAsync(id);
        if (detail == null)
        {
            return NotFound();
        }

        return PartialView("_DetailModal", new UserDetailViewModel { Detail = detail });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(string id)
    {
        var result = await _userService.ToggleActiveAsync(id);
        await WriteAuditLogAsync("ToggleUserActive", $"Toggle active user {id}", result.Success, result.Message);

        return Json(new { success = result.Success, message = result.Message, isActive = result.CurrentState });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkSetActive(string ids, bool isActive)
    {
        var idList = ids
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var result = await _userService.SetActiveBulkAsync(idList, isActive);
        await WriteAuditLogAsync("BulkToggleUserActive", $"Bulk toggle users count={idList.Count}", result.Success, result.Message);

        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _userService.DeleteUserAsync(id);
        await WriteAuditLogAsync("DeleteUser", $"Xóa user {id}", result.Success, result.Message);

        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel([FromQuery] UserFilterViewModel filter, [FromQuery] string? ids)
    {
        var idList = string.IsNullOrWhiteSpace(ids)
            ? null
            : ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        var bytes = await _userService.ExportUsersExcelAsync(BuildFilterDto(filter), idList);
        var fileName = $"users-{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    private UserFilterDto BuildFilterDto(UserFilterViewModel filter, string? fallbackSearch = null, string? sortBy = null, string? sortDirection = null)
    {
        bool? isActive = filter.ActiveStatus?.ToLower() switch
        {
            "active" => true,
            "inactive" => false,
            _ => null
        };

        return new UserFilterDto
        {
            SearchTerm = string.IsNullOrWhiteSpace(filter.SearchTerm) ? fallbackSearch : filter.SearchTerm,
            Role = filter.Role,
            IsActive = isActive,
            CreatedFrom = filter.CreatedFrom,
            CreatedTo = filter.CreatedTo,
            SortBy = sortBy,
            SortDirection = sortDirection
        };
    }

    private static int ParseInt(string? value, int defaultValue)
    {
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    private static bool ValidatePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return true;
        }

        return Regex.IsMatch(phone.Trim(), @"^(0\d{9}|0\d{2}\.\d{3}\.\d{4})$");
    }

    private async Task<string?> SaveAvatarAsync(IFormFile? avatarFile, string? fallback)
    {
        if (avatarFile == null || avatarFile.Length == 0)
        {
            return fallback;
        }

        var extension = Path.GetExtension(avatarFile.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedImageExtensions.Contains(extension))
        {
            return null;
        }

        var folder = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
        Directory.CreateDirectory(folder);

        var fileName = $"avatar-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(folder, fileName);

        await using var stream = System.IO.File.Create(filePath);
        await avatarFile.CopyToAsync(stream);

        return $"/uploads/avatars/{fileName}";
    }

    private async Task WriteAuditLogAsync(string action, string description, bool success, string message)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            await _auditService.LogActionAsync(
                userId: userId,
                action: action,
                entityType: "User",
                entityId: null,
                description: description,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                isSuccessful: success,
                errorMessage: success ? null : message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể ghi audit log cho action {Action}", action);
        }
    }

}
