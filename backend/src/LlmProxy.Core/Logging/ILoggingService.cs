// src/LlmProxy.Core/Logging/ILoggingService.cs
using LlmProxy.Core.Entities;
using LlmProxy.Core.Models.Dto;
using System.Threading;
using System.Threading.Tasks;

namespace LlmProxy.Core.Logging;

public interface ILoggingService
{
    /// <summary>
    /// Асинхронно записывает лог запроса (не блокирует основной поток)
    /// </summary>
    Task LogRequestAsync(
        string apiKeyHash,
        string providerName,
        string modelRequested,
        string modelUsed,
        int latencyMs,
        string status,
        ChatCompletionResponse? response = null,
        Exception? error = null,
        bool isStreaming = false,
        CancellationToken ct = default);

    /// <summary>
    /// Обновляет существующий лог (для streaming: добавляет токены после завершения)
    /// </summary>
    Task UpdateLogAsync(Guid logId, int? tokensPrompt, int? tokensCompletion, CancellationToken ct = default);

    /// <summary>
    /// Получает статистику за период (для Dashboard)
    /// </summary>
    Task<LogStats> GetStatsAsync(DateTime from, DateTime to, CancellationToken ct = default);
}

public record LogStats
{
    public int TotalRequests { get; init; }
    public int SuccessCount { get; init; }
    public int ErrorCount { get; init; }
    public double AvgLatencyMs { get; init; }
    public long TotalPromptTokens { get; init; }
    public long TotalCompletionTokens { get; init; }
    public Dictionary<string, int> RequestsByModel { get; init; } = new();
    public Dictionary<string, int> RequestsByProvider { get; init; } = new();
}