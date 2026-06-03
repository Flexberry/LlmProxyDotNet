using LlmProxy.Core.Entities;
using LlmProxy.Core.Models.Dto;
using System.Threading;
using System.Threading.Tasks;

namespace LlmProxy.Core.Logging;

/// <summary>
/// Service interface for logging LLM proxy requests and operations
/// </summary>
public interface ILoggingService
{
    /// <summary>
    /// Asynchronously logs a request (non-blocking)
    /// </summary>
    /// <param name="apiKeyHash">Hash of the API key used</param>
    /// <param name="providerName">Name of the LLM provider</param>
    /// <param name="modelRequested">Requested model name</param>
    /// <param name="modelUsed">Actual model used</param>
    /// <param name="latencyMs">Request latency in milliseconds</param>
    /// <param name="status">Request status (success/error)</param>
    /// <param name="response">Optional response data</param>
    /// <param name="error">Optional error information</param>
    /// <param name="isStreaming">Whether this is a streaming request</param>
    /// <param name="ct">Cancellation token</param>
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
    /// Updates an existing log entry (for streaming: adds token counts after completion)
    /// </summary>
    /// <param name="logId">ID of the log entry to update</param>
    /// <param name="tokensPrompt">Number of prompt tokens</param>
    /// <param name="tokensCompletion">Number of completion tokens</param>
    /// <param name="ct">Cancellation token</param>
    Task UpdateLogAsync(Guid logId, int? tokensPrompt, int? tokensCompletion, CancellationToken ct = default);

    /// <summary>
    /// Retrieves statistics for a given period (for Dashboard)
    /// </summary>
    /// <param name="from">Start date of the period</param>
    /// <param name="to">End date of the period</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Statistics for the specified period</returns>
    Task<LogStats> GetStatsAsync(DateTime from, DateTime to, CancellationToken ct = default);
}

/// <summary>
/// Statistics record for log data
/// </summary>
public record LogStats
{
    /// <summary>
    /// Total number of requests
    /// </summary>
    public int TotalRequests { get; init; }
    
    /// <summary>
    /// Number of successful requests
    /// </summary>
    public int SuccessCount { get; init; }
    
    /// <summary>
    /// Number of failed requests
    /// </summary>
    public int ErrorCount { get; init; }
    
    /// <summary>
    /// Average latency in milliseconds
    /// </summary>
    public double AvgLatencyMs { get; init; }
    
    /// <summary>
    /// Total prompt tokens used
    /// </summary>
    public long TotalPromptTokens { get; init; }
    
    /// <summary>
    /// Total completion tokens used
    /// </summary>
    public long TotalCompletionTokens { get; init; }
    
    /// <summary>
    /// Request count grouped by model
    /// </summary>
    public Dictionary<string, int> RequestsByModel { get; init; } = new();
    
    /// <summary>
    /// Request count grouped by provider
    /// </summary>
    public Dictionary<string, int> RequestsByProvider { get; init; } = new();
}