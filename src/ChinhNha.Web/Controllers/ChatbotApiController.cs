using ChinhNha.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ChinhNha.Web.Controllers;

/// <summary>
/// API controller cho chatbot khách hàng.
/// POST /api/chatbot/message — gửi tin nhắn, nhận phản hồi AI.
/// GET  /api/chatbot/history  — lấy lịch sử hội thoại.
/// </summary>
[Route("api/chatbot")]
[ApiController]
public class ChatbotApiController : ControllerBase
{
    private readonly IChatbotService _chatbotService;

    public ChatbotApiController(IChatbotService chatbotService)
    {
        _chatbotService = chatbotService;
    }

    [HttpPost("message")]
    public async Task<IActionResult> SendMessage([FromBody] ChatRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Message))
            return BadRequest(new { error = "Tin nhắn không được để trống." });

        // Use session ID from request, or fall back to HTTP session ID
        var sessionId = string.IsNullOrWhiteSpace(req.SessionId)
            ? HttpContext.Session.Id
            : req.SessionId;

        var userEmail = User.Identity?.IsAuthenticated == true ? User.Identity.Name : null;

        var reply = await _chatbotService.ChatAsync(req.Message, sessionId, isAdmin: false, userEmail);
        return Ok(new { reply, sessionId });
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            sessionId = HttpContext.Session.Id;

        var history = await _chatbotService.GetHistoryAsync(sessionId);
        return Ok(history);
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public string? SessionId { get; set; }
    }
}
