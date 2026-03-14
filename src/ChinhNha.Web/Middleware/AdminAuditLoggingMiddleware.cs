using ChinhNha.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace ChinhNha.Web.Middleware;

public class AdminAuditLoggingMiddleware
{
    private static readonly HashSet<string> ExcludedControllers = new(StringComparer.OrdinalIgnoreCase)
    {
        "Media"
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<AdminAuditLoggingMiddleware> _logger;

    public AdminAuditLoggingMiddleware(RequestDelegate next, ILogger<AdminAuditLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuditService auditService)
    {
        var shouldAudit = ShouldAudit(context);

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub")
            ?? context.User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        var routeValues = context.Request.RouteValues;
        var action = routeValues.TryGetValue("action", out var actionValue) ? actionValue?.ToString() : null;
        var entityType = routeValues.TryGetValue("controller", out var controllerValue) ? controllerValue?.ToString() : null;
        var entityId = TryParseEntityId(routeValues.TryGetValue("id", out var idValue) ? idValue?.ToString() : null);
        var description = $"{context.Request.Method} {context.Request.Path}";
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();

        try
        {
            await _next(context);

            if (!shouldAudit || string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(action) || string.IsNullOrWhiteSpace(entityType))
            {
                return;
            }

            await auditService.LogActionAsync(
                userId: userId,
                action: action,
                entityType: entityType,
                entityId: entityId,
                description: description,
                ipAddress: ipAddress,
                isSuccessful: context.Response.StatusCode < StatusCodes.Status400BadRequest,
                errorMessage: context.Response.StatusCode >= StatusCodes.Status400BadRequest ? $"HTTP {context.Response.StatusCode}" : null);
        }
        catch (Exception ex)
        {
            if (shouldAudit && !string.IsNullOrWhiteSpace(userId) && !string.IsNullOrWhiteSpace(action) && !string.IsNullOrWhiteSpace(entityType))
            {
                try
                {
                    await auditService.LogActionAsync(
                        userId: userId,
                        action: action,
                        entityType: entityType,
                        entityId: entityId,
                        description: description,
                        ipAddress: ipAddress,
                        isSuccessful: false,
                        errorMessage: ex.Message);
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "Khong the ghi audit log sau khi request that bai.");
                }
            }

            throw;
        }
    }

    private static bool ShouldAudit(HttpContext context)
    {
        if (HttpMethods.IsHead(context.Request.Method)
            || HttpMethods.IsOptions(context.Request.Method)
            || HttpMethods.IsTrace(context.Request.Method))
        {
            return false;
        }

        if (!context.Request.Path.StartsWithSegments("/Admin", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var controller = context.Request.RouteValues.TryGetValue("controller", out var controllerValue)
            ? controllerValue?.ToString()
            : null;

        if (string.IsNullOrWhiteSpace(controller))
        {
            return false;
        }

        return !ExcludedControllers.Contains(controller);
    }

    private static int? TryParseEntityId(string? id)
    {
        return int.TryParse(id, out var parsed) ? parsed : null;
    }
}
