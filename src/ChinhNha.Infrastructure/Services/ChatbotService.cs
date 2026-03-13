using ChinhNha.Application.DTOs;
using ChinhNha.Application.DTOs.Requests;
using ChinhNha.Application.Interfaces;
using ChinhNha.Domain.Entities;
using ChinhNha.Infrastructure.Data;
using ChinhNha.Infrastructure.Services.Plugins;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

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
    private const string HotlineNumber = "0352.787.350";

    private const string CustomerSystemPrompt =
        "Bạn là trợ lý AI của VTNN Chính Nha — chuyên cung cấp vật tư nông nghiệp tại Cần Thơ. " +
        "Nhiệm vụ: tư vấn sản phẩm phân bón, kiểm tra tồn kho, tra cứu đơn hàng, hướng dẫn sử dụng. " +
        "Trả lời ngắn gọn, thân thiện, bằng tiếng Việt. " +
        "Nếu khách cần liên hệ nhân viên, luôn cung cấp số hotline chính thức: 0352.787.350.";

    private const string AdminSystemPrompt =
        "Bạn là trợ lý AI nội bộ của VTNN Chính Nha dành cho quản trị viên. " +
        "Hỗ trợ: tra cứu đơn hàng, kiểm tra kho, báo cáo doanh thu, gợi ý nhập hàng dựa trên dự báo AI. " +
        "Trả lời chính xác, ngắn gọn, bằng tiếng Việt, tối đa 5 dòng trừ khi người dùng yêu cầu chi tiết. " +
        "Ưu tiên sử dụng dữ liệu thực từ database qua các plugin. Khi cần liên hệ tư vấn, dùng hotline 0352.787.350.";

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

            if (IsGreetingIntent(message))
            {
                var greetingReply = isAdmin
                    ? "Xin chào Admin. Tôi sẵn sàng hỗ trợ dữ liệu nội bộ: doanh thu, đơn hàng, tồn kho, top bán chạy. Bạn có thể hỏi ví dụ: 'doanh thu 7 ngày' hoặc 'sản phẩm sắp hết hàng'."
                    : $"Xin chào. Tôi có thể hỗ trợ tư vấn sản phẩm, kiểm tra tồn kho và tra cứu đơn hàng. Cần gặp tư vấn viên, vui lòng gọi {HotlineNumber}.";
                await PersistMessagesAsync(dbContext, sessionId, message, greetingReply, isAdmin, userEmail);
                return greetingReply;
            }

            // Ưu tiên trả lời trực tiếp từ DB cho các ý định admin phổ biến,
            // giúp không phụ thuộc hoàn toàn vào tool-calling của model.
            if (isAdmin)
            {
                var directReply = await TryHandleAdminDataQueryAsync(message, dbContext);
                if (!string.IsNullOrWhiteSpace(directReply))
                {
                    await PersistMessagesAsync(dbContext, sessionId, message, directReply, isAdmin, userEmail);
                    return directReply;
                }
            }
            else
            {
                var directReply = await TryHandleCustomerDataQueryAsync(message, dbContext);
                if (!string.IsNullOrWhiteSpace(directReply))
                {
                    await PersistMessagesAsync(dbContext, sessionId, message, directReply, isAdmin, userEmail);
                    return directReply;
                }
            }

            // Xây dựng lịch sử hội thoại
            var history = await BuildChatHistoryAsync(dbContext, sessionId, isAdmin);
            history.AddUserMessage(message);

            var runtimeModel = ChooseRuntimeModelForRequest(settings, isAdmin);
            var useGemini = !isAdmin && settings.UseGeminiForUser;
            var geminiApiKey = useGemini ? await _aiModelService.GetGeminiApiKeyAsync() : null;

            _logger.LogInformation(
                "Chat runtime model selected: {RuntimeModel}; provider={Provider}; effective={EffectiveModel}; autoSelect={AutoSelect}; manual={ManualModel}; session={SessionId}",
                runtimeModel,
                useGemini && !string.IsNullOrWhiteSpace(geminiApiKey) ? "Gemini" : "Ollama",
                settings.EffectiveModel,
                settings.AutoSelectEnabled,
                settings.ManualModel,
                sessionId);

            // Xây dựng Kernel với plugins
            var kernel = BuildKernel(
                runtimeModel,
                settings.OllamaEndpoint,
                dbContext,
                isAdmin,
                settings.GeminiModel,
                geminiApiKey,
                useGemini);

            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            var response = await GetChatResponseWithFallbackAsync(chatService, history, kernel, runtimeModel, sessionId);
            var replyText = response.Content ?? "Xin lỗi, tôi không thể xử lý yêu cầu này.";
            replyText = SanitizeAdminReply(message, replyText, isAdmin);
            replyText = EnsureConciseReply(replyText, isAdmin);

            // Lưu vào DB
            await PersistMessagesAsync(dbContext, sessionId, message, replyText, isAdmin, userEmail);

            return replyText;
        }
        catch (Microsoft.SemanticKernel.HttpOperationException ex)
        {
            _logger.LogError(ex, "ChatbotService HTTP error for session {SessionId}", sessionId);

            var errorText = ex.Message ?? string.Empty;
            if (errorText.Contains("requires more system memory", StringComparison.OrdinalIgnoreCase))
            {
                return "⚠️ Model AI hiện tại đang vượt quá RAM khả dụng của máy chủ. " +
                       "Vui lòng chuyển sang model nhẹ hơn (ví dụ sailor2:1b hoặc qwen3:1.7b) rồi thử lại.";
            }

            if (errorText.Contains("model", StringComparison.OrdinalIgnoreCase) &&
                errorText.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return "⚠️ Model AI đang được cấu hình nhưng chưa tồn tại trong Ollama. " +
                       "Hãy cài model tương ứng hoặc chọn model khác trong phần quản trị AI.";
            }

            if (errorText.Contains("does not support tools", StringComparison.OrdinalIgnoreCase))
            {
                return "⚠️ Model AI hiện tại không hỗ trợ tool-calling nên không thể truy vấn dữ liệu nghiệp vụ đầy đủ. " +
                       "Hãy chọn model hỗ trợ tools để chatbot dùng dữ liệu DB chính xác hơn.";
            }

            if ((int?)ex.StatusCode == 429 ||
                errorText.Contains("429", StringComparison.OrdinalIgnoreCase) ||
                errorText.Contains("Too Many Requests", StringComparison.OrdinalIgnoreCase) ||
                errorText.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
            {
                return $"⚠️ Hệ thống AI đã đạt giới hạn yêu cầu (rate limit). " +
                       $"Vui lòng chờ vài giây rồi thử lại, hoặc liên hệ {HotlineNumber} nếu vấn đề kéo dài.";
            }

            return "Rất tiếc, hệ thống AI đang gặp lỗi khi xử lý yêu cầu. Vui lòng thử lại sau ít phút.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ChatbotService error for session {SessionId}", sessionId);
            return "Rất tiếc, đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại hoặc liên hệ nhân viên.";
        }
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(
        string message,
        string sessionId,
        bool isAdmin = false,
        string? userEmail = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            await foreach (var part in StreamTextAsChunksAsync("Vui lòng nhập nội dung tin nhắn.", cancellationToken))
            {
                yield return part;
            }
            yield break;
        }

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var settings = await _aiModelService.GetSettingsAsync();

        if (!settings.OllamaReachable)
        {
            var downReply = "⚠️ Hệ thống AI đang tạm thời không khả dụng (Ollama chưa chạy hoặc phản hồi quá chậm). Vui lòng kiểm tra Ollama rồi thử lại sau.";
            await PersistMessagesAsync(dbContext, sessionId, message, downReply, isAdmin, userEmail);
            await foreach (var part in StreamTextAsChunksAsync(downReply, cancellationToken))
            {
                yield return part;
            }
            yield break;
        }

        if (settings.InstalledModels.Count == 0)
        {
            var noModelReply = "⚠️ Ollama đã chạy nhưng chưa có model nào được cài. Hãy chạy lệnh như 'ollama pull sailor2:1b' hoặc model bạn muốn dùng rồi thử lại.";
            await PersistMessagesAsync(dbContext, sessionId, message, noModelReply, isAdmin, userEmail);
            await foreach (var part in StreamTextAsChunksAsync(noModelReply, cancellationToken))
            {
                yield return part;
            }
            yield break;
        }

        if (IsGreetingIntent(message))
        {
            var greetingReply = isAdmin
                ? "Xin chào Admin. Tôi sẵn sàng hỗ trợ dữ liệu nội bộ: doanh thu, đơn hàng, tồn kho, top bán chạy. Bạn có thể hỏi ví dụ: 'doanh thu 7 ngày' hoặc 'sản phẩm sắp hết hàng'."
                    : $"Xin chào. Tôi có thể hỗ trợ tư vấn sản phẩm, kiểm tra tồn kho và tra cứu đơn hàng. Cần gặp tư vấn viên, vui lòng gọi {HotlineNumber}.";
            await PersistMessagesAsync(dbContext, sessionId, message, greetingReply, isAdmin, userEmail);
            await foreach (var part in StreamTextAsChunksAsync(greetingReply, cancellationToken))
            {
                yield return part;
            }
            yield break;
        }

        if (isAdmin)
        {
            var directReply = await TryHandleAdminDataQueryAsync(message, dbContext);
            if (!string.IsNullOrWhiteSpace(directReply))
            {
                await PersistMessagesAsync(dbContext, sessionId, message, directReply, isAdmin, userEmail);
                await foreach (var part in StreamTextAsChunksAsync(directReply, cancellationToken))
                {
                    yield return part;
                }
                yield break;
            }
        }
        else
        {
            var directReply = await TryHandleCustomerDataQueryAsync(message, dbContext);
            if (!string.IsNullOrWhiteSpace(directReply))
            {
                await PersistMessagesAsync(dbContext, sessionId, message, directReply, isAdmin, userEmail);
                await foreach (var part in StreamTextAsChunksAsync(directReply, cancellationToken))
                {
                    yield return part;
                }
                yield break;
            }
        }

        var history = await BuildChatHistoryAsync(dbContext, sessionId, isAdmin);
        history.AddUserMessage(message);

        var runtimeModel = ChooseRuntimeModelForRequest(settings, isAdmin);
        var useGemini = !isAdmin && settings.UseGeminiForUser;
        var geminiApiKey = useGemini ? await _aiModelService.GetGeminiApiKeyAsync() : null;

        _logger.LogInformation(
            "Stream runtime model selected: {RuntimeModel}; provider={Provider}; effective={EffectiveModel}; autoSelect={AutoSelect}; manual={ManualModel}; session={SessionId}",
            runtimeModel,
            useGemini && !string.IsNullOrWhiteSpace(geminiApiKey) ? "Gemini" : "Ollama",
            settings.EffectiveModel,
            settings.AutoSelectEnabled,
            settings.ManualModel,
            sessionId);

        var kernel = BuildKernel(
            runtimeModel,
            settings.OllamaEndpoint,
            dbContext,
            isAdmin,
            settings.GeminiModel,
            geminiApiKey,
            useGemini);
        var chatService = kernel.GetRequiredService<IChatCompletionService>();

        var responseBuilder = new StringBuilder();
        await foreach (var part in GetChatResponseStreamWithFallbackAsync(
            chatService,
            history,
            kernel,
            runtimeModel,
            sessionId,
            cancellationToken))
        {
            if (string.IsNullOrEmpty(part))
            {
                continue;
            }

            responseBuilder.Append(part);
            yield return part;
        }

        var finalReply = responseBuilder.Length == 0
            ? "Xin lỗi, tôi không thể xử lý yêu cầu này."
            : responseBuilder.ToString();

        finalReply = SanitizeAdminReply(message, finalReply, isAdmin);
        finalReply = EnsureConciseReply(finalReply, isAdmin);

        await PersistMessagesAsync(dbContext, sessionId, message, finalReply, isAdmin, userEmail);
    }

    private static async IAsyncEnumerable<string> StreamTextAsChunksAsync(
        string text,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(text))
        {
            yield break;
        }

        const int chunkSize = 24;
        for (var i = 0; i < text.Length; i += chunkSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var len = Math.Min(chunkSize, text.Length - i);
            yield return text.Substring(i, len);
            await Task.Delay(12, cancellationToken);
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

    private static Kernel BuildKernel(
        string modelId,
        string ollamaEndpoint,
        AppDbContext dbContext,
        bool isAdmin,
        string? geminiModel,
        string? geminiApiKey,
        bool useGemini)
    {
        var kb = Kernel.CreateBuilder();

        if (useGemini && !string.IsNullOrWhiteSpace(geminiApiKey))
        {
            var geminiEndpoint = new Uri("https://generativelanguage.googleapis.com/v1beta/openai/");
#pragma warning disable SKEXP0010
            kb.AddOpenAIChatCompletion(
                modelId: string.IsNullOrWhiteSpace(geminiModel) ? "gemini-2.5-flash" : geminiModel.Trim(),
                apiKey: geminiApiKey.Trim(),
                endpoint: geminiEndpoint);
#pragma warning restore SKEXP0010
        }
        else
        {
            var openAiEndpoint = NormalizeOpenAiCompatibleEndpoint(ollamaEndpoint);
            var runtimeModelId = NormalizeRuntimeModelId(modelId);

#pragma warning disable SKEXP0010
            kb.AddOpenAIChatCompletion(
                modelId: runtimeModelId,
                apiKey: "ollama",
                endpoint: new Uri(openAiEndpoint));
#pragma warning restore SKEXP0010
        }

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

    private static string NormalizeOpenAiCompatibleEndpoint(string endpoint)
    {
        var value = string.IsNullOrWhiteSpace(endpoint)
            ? "http://localhost:11434"
            : endpoint.Trim().TrimEnd('/');

        // Semantic Kernel OpenAI connector expects an OpenAI-style base URL.
        // Ollama serves this API under /v1.
        if (!value.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
        {
            value += "/v1";
        }

        return value;
    }

    private static string NormalizeRuntimeModelId(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return "sailor2:8b";
        }

        return modelId.Trim() switch
        {
            "Sailor2-1B-Chat" => "sailor2:1b",
            "Sailor2-8B-Chat" => "sailor2:8b",
            "Sailor2-20B-Chat" => "sailor2:20b",
            _ => modelId.Trim()
        };
    }

    private static string ChooseRuntimeModelForRequest(AiModelSettingsDto settings, bool isAdmin)
    {
        var effective = settings.EffectiveModel;

        // Tôn trọng model khóa thủ công (áp dụng cho cả admin và user).
        if (!settings.AutoSelectEnabled && !string.IsNullOrWhiteSpace(settings.ManualModel))
        {
            return settings.ManualModel;
        }

        var installed = settings.InstalledModels ?? [];

        // Nếu effective model hiện tại đã là model >= 3B thì dùng luôn, không override.
        if (!effective.Equals("Sailor2-1B-Chat", StringComparison.OrdinalIgnoreCase))
        {
            return effective;
        }

        // Chỉ auto nâng chất lượng khi đang rơi vào 1B ở auto mode.
        var priority = new[]
        {
            "llama3.1:8b",
            "qwen3:8b",
            "Sailor2-8B-Chat",
            "qwen3:4b",
            "llama3.2:3b",
            "Sailor2-1B-Chat",
            effective
        };

        var candidate = priority.FirstOrDefault(p => installed.Contains(p, StringComparer.OrdinalIgnoreCase));
        return string.IsNullOrWhiteSpace(candidate) ? effective : candidate;
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

    private async Task<ChatMessageContent> GetChatResponseWithFallbackAsync(
        IChatCompletionService chatService,
        ChatHistory history,
        Kernel kernel,
        string effectiveModel,
        string sessionId)
    {
        var toolEnabledSettings = new PromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        try
        {
            return await chatService.GetChatMessageContentAsync(history, toolEnabledSettings, kernel);
        }
        catch (Microsoft.SemanticKernel.HttpOperationException ex)
            when (ex.Message.Contains("does not support tools", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Model {Model} does not support tool-calling for session {SessionId}. Falling back to plain chat mode.",
                effectiveModel,
                sessionId);

            // Retry without function calling so end users still get a response.
            return await chatService.GetChatMessageContentAsync(history, new PromptExecutionSettings(), kernel);
        }
    }

    private async IAsyncEnumerable<string> GetChatResponseStreamWithFallbackAsync(
        IChatCompletionService chatService,
        ChatHistory history,
        Kernel kernel,
        string effectiveModel,
        string sessionId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var toolEnabledSettings = new PromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        IAsyncEnumerable<StreamingChatMessageContent> stream;

        try
        {
            stream = chatService.GetStreamingChatMessageContentsAsync(history, toolEnabledSettings, kernel);
        }
        catch (Microsoft.SemanticKernel.HttpOperationException ex)
            when (ex.Message.Contains("does not support tools", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Model {Model} does not support tool-calling for session {SessionId}. Falling back to plain chat streaming mode.",
                effectiveModel,
                sessionId);

            stream = chatService.GetStreamingChatMessageContentsAsync(history, new PromptExecutionSettings(), kernel);
        }

        // Use IAsyncEnumerator directly so exceptions during iteration can be caught
        // without hitting the C# "yield in try-catch" restriction.
        string? streamError = null;
        var enumerator = stream.GetAsyncEnumerator(cancellationToken);
        try
        {
            while (true)
            {
                bool hasNext;
                try
                {
                    hasNext = await enumerator.MoveNextAsync();
                }
                catch (Microsoft.SemanticKernel.HttpOperationException ex)
                    when ((int?)ex.StatusCode == 429 ||
                          ex.Message.Contains("Too Many Requests", StringComparison.OrdinalIgnoreCase) ||
                          ex.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(ex,
                        "Rate limit (429) during stream for session {SessionId} model {Model}",
                        sessionId, effectiveModel);
                    streamError = $"⚠️ Hệ thống AI đã đạt giới hạn yêu cầu (rate limit). " +
                                  $"Vui lòng chờ vài giây rồi thử lại, hoặc liên hệ {HotlineNumber} nếu vấn đề kéo dài.";
                    break;
                }

                if (!hasNext) break;

                var text = enumerator.Current.Content;
                if (!string.IsNullOrEmpty(text))
                {
                    yield return text;
                }
            }
        }
        finally
        {
            await enumerator.DisposeAsync();
        }

        if (streamError != null)
        {
            yield return streamError;
        }
    }

    private static string EnsureConciseReply(string text, bool isAdmin)
    {
        if (!isAdmin || string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        var normalized = text.Replace("\r\n", "\n").Trim();
        var lines = normalized.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (normalized.Length <= 900 && lines.Length <= 8)
        {
            return normalized;
        }

        var compact = string.Join("\n", lines.Take(8));
        return compact + "\n(Đã rút gọn. Bạn có thể yêu cầu xem chi tiết hơn.)";
    }

    private static string SanitizeAdminReply(string userMessage, string assistantReply, bool isAdmin)
    {
        if (!isAdmin || string.IsNullOrWhiteSpace(assistantReply))
        {
            return assistantReply;
        }

        var user = NormalizeIntentText(userMessage);
        var reply = NormalizeIntentText(assistantReply);

        // Guardrail cho trường hợp model trả lời lạc đề dài dòng khi người dùng chỉ chào hỏi.
        if (IsGreetingIntent(userMessage))
        {
            return "Xin chào Admin. Tôi đang hoạt động bình thường. Bạn cần xem nhanh doanh thu, tồn kho hay đơn hàng?";
        }

        var suspiciousOffTopic = ContainsAny(reply,
            "marketplace", "seo", "social media", "quảng cáo", "tiếp thị", "truyền thông");
        var userAskedBusinessData = IsLowStockIntent(user) || IsRevenueIntent(user) || IsTopSellingIntent(user) || IsOrderStatusIntent(user);

        if (suspiciousOffTopic && userAskedBusinessData)
        {
            return "Tôi chưa trả lời đúng trọng tâm dữ liệu nội bộ. Vui lòng hỏi lại theo mẫu ngắn: 'doanh thu 7 ngày', 'top 5 bán chạy', 'sản phẩm sắp hết hàng', hoặc 'trạng thái đơn 123'.";
        }

        return assistantReply;
    }

    private static int? ExtractFirstInteger(string message)
    {
        var match = Regex.Match(message, @"\d+");
        if (!match.Success)
        {
            return null;
        }

        return int.TryParse(match.Value, out var value) ? value : null;
    }

    private static bool ContainsAny(string source, params string[] keywords)
        => keywords.Any(source.Contains);

    private static bool IsLowStockIntent(string text)
        => ContainsAny(text,
            "sap het", "sắp hết", "het hang", "hết hàng", "ton kho thap", "tồn kho thấp", "thieu hang", "thiếu hàng");

    private static bool IsRevenueIntent(string text)
        => ContainsAny(text,
            "doanh thu", "revenue", "bao cao", "báo cáo", "don hang", "đơn hàng") &&
           !ContainsAny(text, "top", "ban chay", "bán chạy");

    private static bool IsTopSellingIntent(string text)
        => ContainsAny(text,
            "top", "ban chay", "bán chạy", "san pham ban chay", "sản phẩm bán chạy");

    private static bool IsOrderStatusIntent(string text)
        => ContainsAny(text,
            "don", "đơn", "ma don", "mã đơn", "trang thai", "trạng thái");

    private static bool IsGreetingIntent(string text)
    {
        var value = NormalizeIntentText(text);
        return value is "hi" or "hello" or "alo" or "chao" or "xin chao" or "chao admin" or "chao ban"
               || value.StartsWith("xin chao")
               || value.StartsWith("chao admin")
               || value.StartsWith("chao");
    }

    private async Task<string?> TryHandleAdminDataQueryAsync(string message, AppDbContext dbContext)
    {
        var text = NormalizeIntentText(message);
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var reportPlugin = new ReportPlugin(dbContext);

        if (IsLowStockIntent(text))
        {
            return await reportPlugin.GetLowStockProductsAsync();
        }

        if (IsTopSellingIntent(text))
        {
            var top = ExtractFirstInteger(text) ?? 5;
            top = Math.Clamp(top, 1, 20);
            return await reportPlugin.GetTopSellingProductsAsync(top);
        }

        if (IsRevenueIntent(text))
        {
            var days = ExtractFirstInteger(text) ?? 7;
            days = Math.Clamp(days, 1, 365);
            return await reportPlugin.GetSalesSummaryAsync(days);
        }

        if (IsOrderStatusIntent(text))
        {
            var orderId = ExtractFirstInteger(text);
            if (orderId.HasValue)
            {
                var orderPlugin = new OrderPlugin(dbContext);
                return await orderPlugin.GetOrderStatusAsync(orderId.Value);
            }
        }

        return null;
    }

    private static bool IsStockCheckIntent(string text)
        => ContainsAny(text, "con hang", "còn hàng", "het hang", "hết hàng", "ton kho", "tồn kho", "sap het", "sắp hết");

    private static bool IsPriceCheckIntent(string text)
        => ContainsAny(text, "gia", "giá", "bao nhieu", "bao nhiêu", "gia bao nhieu", "giá bao nhiêu");

    private async Task<string?> TryHandleCustomerDataQueryAsync(string message, AppDbContext dbContext)
    {
        var text = NormalizeIntentText(message);
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var inventoryPlugin = new InventoryPlugin(dbContext);

        if (IsStockCheckIntent(text))
        {
            var keyword = ExtractProductKeyword(text);
            if (string.IsNullOrWhiteSpace(keyword)) return null;
            return await inventoryPlugin.CheckStockByNameAsync(keyword);
        }

        if (IsPriceCheckIntent(text))
        {
            var keyword = ExtractProductKeyword(text);
            if (string.IsNullOrWhiteSpace(keyword)) return null;
            return await inventoryPlugin.GetProductInfoAsync(keyword);
        }

        if (ContainsAny(text, "tu van", "tư vấn", "goi y", "gợi ý"))
        {
            var keyword = ExtractProductKeyword(text);
            return await inventoryPlugin.SuggestProductsAsync(string.IsNullOrWhiteSpace(keyword) ? text : keyword);
        }

        return null;
    }

    // Strips intent noise from a normalized (no-diacritic, lowercase) query so only
    // the product keyword remains, e.g. "phan ure 46% con bao nhieu hang" -> "phan ure 46%".
    private static string ExtractProductKeyword(string normalizedText)
    {
        if (string.IsNullOrWhiteSpace(normalizedText))
            return normalizedText;

        var s = normalizedText.Trim().TrimEnd('?', '!', '.').Trim();

        // Strip leading intent verbs
        s = Regex.Replace(s,
            @"^(tu van|goi y|kiem tra ton kho|kiem tra gia|kiem tra|tra cuu|cho toi biet|cho biet|hien co|ban co)\s+",
            string.Empty);

        // Strip trailing stock/price noise phrases — longest first to avoid partial matches
        string[] trailingNoise =
        [
            "con bao nhieu hang khong", "co bao nhieu hang khong", "co bao nhieu hang",
            "con bao nhieu hang", "co con hang khong", "con hang khong", "co hang khong",
            "het hang chua", "con bao nhieu", "bao nhieu hang", "ton kho con bao nhieu",
            "ton kho thap", "sap het hang", "con hang", "het hang", "ton kho", "sap het",
            "la bao nhieu", "gia bao nhieu", "bao nhieu tien", "bao nhieu dong",
            "bao nhieu", "gia ca", "con khong", "co khong",
        ];

        foreach (var noise in trailingNoise)
        {
            if (s.EndsWith(noise, StringComparison.OrdinalIgnoreCase))
            {
                s = s[..^noise.Length].TrimEnd();
                break;
            }
        }

        // Strip standalone leading "gia" (price queries like "gia [product]")
        s = Regex.Replace(s, @"^gia\s+", string.Empty);

        return s.Trim();
    }

    private static string NormalizeIntentText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        // Remove zero-width chars and normalize Unicode form before intent matching.
        var cleaned = text
            .Replace("\u200B", string.Empty)
            .Replace("\u200C", string.Empty)
            .Replace("\u200D", string.Empty)
            .Replace("\uFEFF", string.Empty)
            .Normalize(NormalizationForm.FormD);

        var sb = new System.Text.StringBuilder(cleaned.Length);
        foreach (var c in cleaned)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            sb.Append(char.IsPunctuation(c) ? ' ' : char.ToLowerInvariant(c));
        }

        return Regex.Replace(sb.ToString(), "\\s+", " ").Trim();
    }
}
