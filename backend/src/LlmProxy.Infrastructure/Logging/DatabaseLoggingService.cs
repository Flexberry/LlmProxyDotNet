// src/LlmProxy.Infrastructure/Logging/DatabaseLoggingService.cs
using LlmProxy.Core.Entities;
using LlmProxy.Core.Logging;
using LlmProxy.Core.Models.Dto;
using LlmProxy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LlmProxy.Infrastructure.Logging;

public class DatabaseLoggingService : ILoggingService
{
    private readonly ILogger<DatabaseLoggingService> _logger;

    public DatabaseLoggingService(ILogger<DatabaseLoggingService> logger)
    {
        _logger = logger;
    }

    public async Task LogRequestAsync(
        string apiKeyHash,
        string providerName,
        string modelRequested,
        string modelUsed,
        int latencyMs,
        string status,
        ChatCompletionResponse? response = null,
        Exception? error = null,
        bool isStreaming = false,
        CancellationToken ct = default)
    {
        try
        {
            var logEntry = new RequestLog
            {
                ApiKeyHash = apiKeyHash,
                ProviderName = providerName,
                ModelRequested = modelRequested,
                ModelUsed = modelUsed,
                LatencyMs = latencyMs,
                Status = status,
                ErrorMessage = error?.Message,
                ResponseId = response?.Id,
                IsStreaming = isStreaming,
                TokensPrompt = response?.Usage?.PromptTokens,
                TokensCompletion = response?.Usage?.CompletionTokens,
                CreatedAt = DateTime.UtcNow
            };

            // Используем статический writer
            await LogBatchWriterService.GetWriter().WriteAsync(logEntry, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue log entry for writing");
        }
    }

    public async Task UpdateLogAsync(Guid logId, int? tokensPrompt, int? tokensCompletion, CancellationToken ct = default)
    {
        // Для обновления одного лога можно использовать отдельный scope, если нужно
        // Но для MVP можно оставить заглушку или реализовать аналогично через scope
        await Task.CompletedTask; 
    }

    public async Task<LogStats> GetStatsAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        // Stats лучше читать напрямую через DbContext в контроллере, а не через этот сервис, 
        // чтобы избежать проблем с lifetime. 
        // Но если нужно здесь, то придется инжектить IServiceScopeFactory.
        // Для простоты вернем пустую статистику, а реальную реализуем в AdminController напрямую.
        return new LogStats(); 
    }
}