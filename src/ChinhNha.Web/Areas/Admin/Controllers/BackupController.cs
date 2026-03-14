using ChinhNha.Application.Interfaces;
using ChinhNha.Web.Areas.Admin.Models;
using ChinhNha.Web.Areas.Admin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChinhNha.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class BackupController : Controller
{
    private readonly IDatabaseBackupService _backupService;
    private readonly IAuditService _auditService;
    private readonly ILogger<BackupController> _logger;

    public BackupController(
        IDatabaseBackupService backupService,
        IAuditService auditService,
        ILogger<BackupController> logger)
    {
        _backupService = backupService;
        _auditService = auditService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = new DatabaseBackupViewModel
        {
            BackupDirectory = _backupService.GetBackupDirectory(),
            Backups = (await _backupService.GetBackupsAsync()).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBackup()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        try
        {
            var fileName = await _backupService.CreateBackupAsync("manual");
            TempData["SuccessMessage"] = $"Đã tạo bản sao lưu thành công: {fileName}";

            if (!string.IsNullOrWhiteSpace(userId))
            {
                await _auditService.LogActionAsync(
                    userId: userId,
                    action: "CreateBackup",
                    entityType: "Database",
                    entityId: null,
                    description: $"Tạo backup thủ công: {fileName}",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo backup database.");
            TempData["ErrorMessage"] = $"Không thể tạo bản sao lưu. {ex.Message}";

            if (!string.IsNullOrWhiteSpace(userId))
            {
                await _auditService.LogActionAsync(
                    userId: userId,
                    action: "CreateBackup",
                    entityType: "Database",
                    entityId: null,
                    description: "Tạo backup thủ công",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    isSuccessful: false,
                    errorMessage: ex.Message);
            }
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(string backupFileName)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(backupFileName))
        {
            TempData["ErrorMessage"] = "Tên file sao lưu không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var result = await _backupService.RestoreAsync(backupFileName);
            TempData["SuccessMessage"] =
                $"Khôi phục thành công từ {result.RestoredFromFile}. Hệ thống đã tạo bản sao lưu an toàn trước khi khôi phục: {result.PreRestoreBackupFile}.";

            if (!string.IsNullOrWhiteSpace(userId))
            {
                await _auditService.LogActionAsync(
                    userId: userId,
                    action: "RestoreBackup",
                    entityType: "Database",
                    entityId: null,
                    description: $"Khôi phục DB từ {result.RestoredFromFile}, backup an toàn {result.PreRestoreBackupFile}",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi khôi phục database từ file {BackupFileName}", backupFileName);
            TempData["ErrorMessage"] =
                "Không thể khôi phục cơ sở dữ liệu. Hãy kiểm tra quyền SQL RESTORE, quyền truy cập file backup và thử lại.";

            if (!string.IsNullOrWhiteSpace(userId))
            {
                await _auditService.LogActionAsync(
                    userId: userId,
                    action: "RestoreBackup",
                    entityType: "Database",
                    entityId: null,
                    description: $"Khôi phục DB từ {backupFileName}",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    isSuccessful: false,
                    errorMessage: ex.Message);
            }
        }

        return RedirectToAction(nameof(Index));
    }
}
