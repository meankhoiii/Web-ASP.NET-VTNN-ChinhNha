using ChinhNha.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ChinhNha.Web.Areas.Admin.Controllers;

/// <summary>
/// Admin chatbot AI nội bộ — trợ lý hỏi báo cáo, kho, đơn hàng bằng ngôn ngữ tự nhiên.
/// Route: /Admin/Chatbot
/// </summary>
[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class ChatbotController : Controller
{
    private readonly IChatbotService _chatbotService;

    public ChatbotController(IChatbotService chatbotService)
    {
        _chatbotService = chatbotService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>API endpoint: admin gửi tin nhắn tới chatbot.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Admin/Chatbot/Send")]
    public async Task<IActionResult> Send([FromBody] AdminChatRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Message))
            return BadRequest(new { error = "Tin nhắn không được để trống." });

        var sessionId = string.IsNullOrWhiteSpace(req.SessionId)
            ? HttpContext.Session.Id + "_admin"
            : req.SessionId;

        var adminEmail = User.Identity?.Name;
        var reply = await _chatbotService.ChatAsync(req.Message, sessionId, isAdmin: true, adminEmail);
        return Ok(new { reply, sessionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Admin/Chatbot/Stream")]
    public async Task Stream([FromBody] AdminChatRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Message))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsync("{\"type\":\"error\",\"message\":\"Tin nhắn không được để trống.\"}\n");
            return;
        }

        var sessionId = string.IsNullOrWhiteSpace(req.SessionId)
            ? HttpContext.Session.Id + "_admin"
            : req.SessionId;

        var adminEmail = User.Identity?.Name;

        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "application/x-ndjson; charset=utf-8";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Append("X-Accel-Buffering", "no");

        await WriteNdjsonAsync(new { type = "meta", sessionId });

        try
        {
            await foreach (var chunk in _chatbotService.ChatStreamAsync(
                req.Message,
                sessionId,
                isAdmin: true,
                adminEmail,
                HttpContext.RequestAborted))
            {
                await WriteNdjsonAsync(new { type = "chunk", content = chunk });
            }

            await WriteNdjsonAsync(new { type = "done" });
        }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            // Client disconnected.
        }
        catch (Exception ex)
        {
            await WriteNdjsonAsync(new { type = "error", message = "Lỗi stream phản hồi AI.", detail = ex.Message });
        }

        async Task WriteNdjsonAsync(object payload)
        {
            var line = JsonSerializer.Serialize(payload);
            await Response.WriteAsync(line + "\n");
            await Response.Body.FlushAsync();
        }
    }

    public class AdminChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public string? SessionId { get; set; }
    }
}
