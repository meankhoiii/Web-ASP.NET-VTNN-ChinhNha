using ChinhNha.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    public class AdminChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public string? SessionId { get; set; }
    }
}
