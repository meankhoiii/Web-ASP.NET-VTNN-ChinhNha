using ChinhNha.Application.DTOs;

namespace ChinhNha.Application.Interfaces;

/// <summary>
/// Chatbot service dùng Semantic Kernel + Ollama (LLM local miễn phí).
/// Hỗ trợ hai mode: khách hàng (tư vấn sản phẩm/đơn hàng) và admin (báo cáo/kho).
/// Tự động degradation nếu Ollama không khả dụng.
/// </summary>
public interface IChatbotService
{
    /// <summary>
    /// Gửi tin nhắn và nhận phản hồi AI.
    /// </summary>
    /// <param name="message">Nội dung tin nhắn người dùng.</param>
    /// <param name="sessionId">ID phiên để duy trì lịch sử hội thoại.</param>
    /// <param name="isAdmin">true nếu là admin chatbot (mở thêm plugin báo cáo).</param>
    /// <param name="userEmail">Email người dùng đã đăng nhập (null nếu khách).</param>
    Task<string> ChatAsync(string message, string sessionId, bool isAdmin = false, string? userEmail = null);

    /// <summary>
    /// Gửi tin nhắn và nhận phản hồi AI theo dạng stream từng phần nội dung.
    /// </summary>
    IAsyncEnumerable<string> ChatStreamAsync(
        string message,
        string sessionId,
        bool isAdmin = false,
        string? userEmail = null,
        CancellationToken cancellationToken = default);

    /// <summary>Lấy lịch sử hội thoại của một phiên.</summary>
    Task<IEnumerable<ChatMessageDto>> GetHistoryAsync(string sessionId, int limit = 20);
}
