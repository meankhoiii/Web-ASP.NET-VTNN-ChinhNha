using ChinhNha.Application.DTOs;
using ChinhNha.Application.Interfaces;
using ChinhNha.Domain.Entities;
using ChinhNha.Infrastructure.Data;
using ChinhNha.Infrastructure.Services.Plugins;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace ChinhNha.Infrastructure.Services;

/// <summary>
/// Chatbot service sử dụng Semantic Kernel + Ollama (OpenAI-compatible API).
/// - Tự động fallback nếu Ollama không chạy (trả về thông báo thân thiện).
/// - Lưu lịch sử hội thoại vào DB.
/// - Plugin: InventoryPlugin (khách/admin), OrderPlugin (khách/admin), ReportPlugin (admin only).
/// </summary>
public class ChatbotService : IChatbotService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAiModelSelectionService _aiModelService;
    private readonly ILogger<ChatbotService> _logger;

    private const string CustomerSystemPrompt =
        "Bạn là trợ lý AI của cửa hàng phân bón Chính Nha — chuyên cung cấp vật tư nông nghiệp tại Cần Thơ. " +
        "Nhiệm vụ: tư vấn sản phẩm phân bón, kiểm tra tồn kho, tra cứu đơn hàng, hướng dẫn sử dụng. " +
        "Trả lời ngắn gọn, thân thiện, bằng tiếng Việt. " +
        "Nếu không biết, hãy đề nghị khách liên hệ nhân viên qua hotline.";

    private const string AdminSystemPrompt =
        "Bạn là trợ lý AI nội bộ của cửa hàng phân bón Chính Nha dành cho quản trị viên. " +
        "Hỗ trợ: tra cứu đơn hàng, kiểm tra kho, báo cáo doanh thu, gợi ý nhập hàng dựa trên dự báo AI. " +
        "Trả lời chính xác, ngắn gọn, bằng tiếng Việt. " +
        "Ưu tiên sử dụng dữ liệu thực từ database qua các plugin.";

    public ChatbotService(
        IServiceScopeFactory scopeFactory,
        IAiModelSelectionService aiModelService,
        ILogger<ChatbotService> logger)
    {
        _scopeFactory = scopeFactory;
        _aiModelService = aiModelService;
        _logger = logger;
    }

    public async Task<string> ChatAsync(
        string message, string sessionId, bool isAdmin = false, string? userEmail = null)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "Vui lòng nhập nội dung tin nhắn.";

        try
        {
            var settings = await _aiModelService.GetSettingsAsync();

            if (!settings.OllamaReachable)
            {
                return "⚠️ Hệ thống AI đang tạm thời không khả dụng (Ollama chưa chạy hoặc phản hồi quá chậm). " +
                       "Vui lòng kiểm tra Ollama rồi thử lại sau.";
            }

            if (settings.InstalledModels.Count == 0)
            {
                return "⚠️ Ollama đã chạy nhưng chưa có model nào được cài. " +
                       "Hãy chạy lệnh như 'ollama pull sailor2:1b' hoặc model bạn muốn dùng rồi thử lại.";
            }

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Xây dựng lịch sử hội thoại
            var history = await BuildChatHistoryAsync(dbContext, sessionId, isAdmin);
            history.AddUserMessage(message);

            // Xây dựng Kernel với plugins
            var kernel = BuildKernel(settings.EffectiveModel, settings.OllamaEndpoint, dbContext, isAdmin);

            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            var executionSettings = new PromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

            var response = await chatService.GetChatMessageContentAsync(history, executionSettings, kernel);
            var replyText = response.Content ?? "Xin lỗi, tôi không thể xử lý yêu cầu này.";

            // Lưu vào DB
            await PersistMessagesAsync(dbContext, sessionId, message, replyText, isAdmin, userEmail);

            return replyText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ChatbotService error for session {SessionId}", sessionId);
            return "Rất tiếc, đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại hoặc liên hệ nhân viên.";
        }
    }

    public async Task<IEnumerable<ChatMessageDto>> GetHistoryAsync(string sessionId, int limit = 20)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.ChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ChatMessageDto
            {
                Role = m.Role,
                Content = m.Content,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync();
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static Kernel BuildKernel(string modelId, string ollamaEndpoint, AppDbContext dbContext, bool isAdmin)
    {
#pragma warning disable SKEXP0010
        var kb = Kernel.CreateBuilder()
            .AddOpenAIChatCompletion(
                modelId: modelId,
                apiKey: "ollama",
                endpoint: new Uri(ollamaEndpoint));
#pragma warning restore SKEXP0010

        // Plugins dùng chung (khách hàng + admin)
        kb.Plugins.AddFromObject(new InventoryPlugin(dbContext), "Inventory");
        kb.Plugins.AddFromObject(new OrderPlugin(dbContext), "Order");

        // Plugin chỉ dành cho admin
        if (isAdmin)
        {
            kb.Plugins.AddFromObject(new ReportPlugin(dbContext), "Report");
        }

        return kb.Build();
    }

    private static async Task<ChatHistory> BuildChatHistoryAsync(
        AppDbContext db, string sessionId, bool isAdmin)
    {
        var systemPrompt = isAdmin ? AdminSystemPrompt : CustomerSystemPrompt;
        var history = new ChatHistory(systemPrompt);

        var past = await db.ChatMessages
            .Where(m => m.SessionId == sessionId && m.IsAdmin == isAdmin)
            .OrderBy(m => m.CreatedAt)
            .Take(20) // Giữ tối đa 20 lượt gần nhất để tiết kiệm context
            .ToListAsync();

        foreach (var msg in past)
        {
            if (msg.Role == "user")
                history.AddUserMessage(msg.Content);
            else
                history.AddAssistantMessage(msg.Content);
        }

        return history;
    }

    private static async Task PersistMessagesAsync(
        AppDbContext db, string sessionId, string userMsg, string assistantMsg,
        bool isAdmin, string? userEmail)
    {
        db.ChatMessages.AddRange(
            new ChatMessage
            {
                SessionId = sessionId,
                UserEmail = userEmail,
                IsAdmin = isAdmin,
                Role = "user",
                Content = userMsg
            },
            new ChatMessage
            {
                SessionId = sessionId,
                UserEmail = userEmail,
                IsAdmin = isAdmin,
                Role = "assistant",
                Content = assistantMsg
            }
        );
        await db.SaveChangesAsync();
    }
}
