namespace ChinhNha.Domain.Entities;

/// <summary>
/// Lưu lịch sử hội thoại chatbot AI (cả khách hàng và admin).
/// SessionId dùng để nhóm các tin nhắn theo một phiên trò chuyện.
/// </summary>
public class ChatMessage
{
    public int Id { get; set; }

    /// <summary>Phiên hội thoại (UUID). Guest + logged-in user đều có.</summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>Email người dùng (null nếu là khách vãng lai).</summary>
    public string? UserEmail { get; set; }

    /// <summary>true nếu tin nhắn này thuộc phiên admin chatbot.</summary>
    public bool IsAdmin { get; set; }

    /// <summary>"user" hoặc "assistant".</summary>
    public string Role { get; set; } = "user";

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
