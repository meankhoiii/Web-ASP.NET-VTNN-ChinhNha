using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ChinhNha.Application.Interfaces;
using ChinhNha.Application.DTOs.Admin;

namespace ChinhNha.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class AuditController : Controller
{
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditController> _logger;

    public AuditController(IAuditService auditService, ILogger<AuditController> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 50)
    {
        try
        {
            var logs = await _auditService.GetAuditLogsAsync(page, pageSize);
            var stats = await _auditService.GetAuditLogStatsAsync();
            var total = await _auditService.GetTotalAuditLogsCountAsync();

            ViewBag.AuditStats = stats;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (total + pageSize - 1) / pageSize;
            ViewBag.TotalLogs = total;

            return View(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loi khi tai nhat ky he thong");
            return BadRequest("Không thể tải nhật ký hệ thống.");
        }
    }

    [HttpGet]
    public async Task<IActionResult> ByUser(string userId, int page = 1)
    {
        try
        {
            var logs = await _auditService.GetAuditLogsByUserAsync(userId, page, 50);
            ViewBag.UserId = userId;
            ViewBag.CurrentPage = page;

            return View("Index", logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loi khi tai nhat ky theo nguoi dung");
            return BadRequest("Không thể tải nhật ký theo người dùng.");
        }
    }

    [HttpGet]
    public async Task<IActionResult> ByAction(string action, int page = 1)
    {
        try
        {
            var logs = await _auditService.GetAuditLogsByActionAsync(action, page, 50);
            ViewBag.FilterAction = action;
            ViewBag.CurrentPage = page;

            return View("Index", logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loi khi tai nhat ky theo thao tac");
            return BadRequest("Không thể tải nhật ký theo thao tác.");
        }
    }

    [HttpGet]
    public async Task<IActionResult> ByEntity(string entityType, int? entityId = null, int page = 1)
    {
        try
        {
            var logs = await _auditService.GetAuditLogsByEntityAsync(entityType, entityId, page, 50);
            ViewBag.EntityType = entityType;
            ViewBag.EntityId = entityId;
            ViewBag.CurrentPage = page;

            return View("Index", logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loi khi tai nhat ky theo doi tuong");
            return BadRequest("Không thể tải nhật ký theo đối tượng.");
        }
    }

    [HttpGet]
    public async Task<IActionResult> Filter(DateTime? fromDate, DateTime? toDate, string? action, string? entityType, int page = 1)
    {
        try
        {
            IEnumerable<AuditLogDto> logs;

            if (fromDate.HasValue && toDate.HasValue)
            {
                logs = await _auditService.GetAuditLogsByDateRangeAsync(fromDate.Value, toDate.Value, page, 50);
            }
            else if (!string.IsNullOrEmpty(action))
            {
                logs = await _auditService.GetAuditLogsByActionAsync(action, page, 50);
            }
            else if (!string.IsNullOrEmpty(entityType))
            {
                logs = await _auditService.GetAuditLogsByEntityAsync(entityType, null, page, 50);
            }
            else
            {
                logs = await _auditService.GetAuditLogsAsync(page, 50);
            }

            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.FilterAction = action;
            ViewBag.FilterEntityType = entityType;
            ViewBag.CurrentPage = page;

            return View("Index", logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loi khi loc nhat ky he thong");
            return BadRequest("Không thể lọc nhật ký hệ thống.");
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var log = await _auditService.GetAuditLogByIdAsync(id);
            if (log == null)
                return NotFound();

            return View(log);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loi khi tai chi tiet nhat ky");
            return BadRequest("Không thể tải chi tiết nhật ký.");
        }
    }

    [HttpPost]
    public IActionResult ExportCsv(DateTime? fromDate, DateTime? toDate)
    {
        try
        {
            // TODO: Implement CSV export
            return BadRequest("Chức năng xuất CSV đang được phát triển.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Loi khi xuat nhat ky he thong");
            return BadRequest("Không thể xuất nhật ký hệ thống.");
        }
    }
}
