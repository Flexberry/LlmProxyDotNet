using System.Text.Json;
using LlmProxy.Core.Entities;
using Microsoft.Extensions.Logging;

namespace LlmProxy.Infrastructure.Services;

/// <summary>
/// Сервис для управления webhook событиями
/// </summary>
public class WebhookService : IWebhookService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public WebhookService(HttpClient httpClient, ILogger<WebhookService> logger, IServiceProvider serviceProvider)
    {
        _httpClient = httpClient;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task<bool> SendWebhookAsync(string webhookUrl, WebhookEventType eventType, object data, CancellationToken ct = default)
    {
        try
        {
            var payload = new
            {
                eventType = eventType.ToString(),
                timestamp = DateTime.UtcNow,
                data
            };
            
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(webhookUrl, content, ct);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Webhook sent successfully to: {Url}", webhookUrl);
                return true;
            }
            
            _logger.LogWarning("Webhook failed with status: {StatusCode} for url: {Url}", response.StatusCode, webhookUrl);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending webhook to: {Url}", webhookUrl);
            return false;
        }
    }

    public async Task<bool> SendRequestSuccessWithUrlAsync(string webhookUrl, string requestId, string model, string provider, int tokensUsed, decimal cost, CancellationToken ct = default)
    {
        return await SendWebhookAsync(webhookUrl, WebhookEventType.RequestSuccess, new
        {
            requestId,
            model,
            provider,
            tokensUsed,
            cost,
            status = "success"
        }, ct);
    }

    public async Task<bool> SendRequestErrorWithUrlAsync(string webhookUrl, string requestId, string model, string error, CancellationToken ct = default)
    {
        return await SendWebhookAsync(webhookUrl, WebhookEventType.RequestError, new
        {
            requestId,
            model,
            error,
            status = "error"
        }, ct);
    }

    public async Task<bool> SendRateLimitExceededWithUrlAsync(string webhookUrl, string apiKeyHash, string limitType, int currentValue, int limit, CancellationToken ct = default)
    {
        return await SendWebhookAsync(webhookUrl, WebhookEventType.RateLimitExceeded, new
        {
            apiKeyHash,
            limitType,
            currentValue,
            limit,
            timestamp = DateTime.UtcNow
        }, ct);
    }

    public async Task<bool> SendBudgetExceededWithUrlAsync(string webhookUrl, string entityId, string entityType, decimal currentSpending, decimal budgetAmount, CancellationToken ct = default)
    {
        return await SendWebhookAsync(webhookUrl, WebhookEventType.BudgetExceeded, new
        {
            entityId, 
            entityType, 
            currentSpending,
            budgetAmount,
            timestamp = DateTime.UtcNow
        }, ct);
    }

    /// <summary>
    /// Отправляет webhook об успешном запросе (для RateLimitEnforcerService)
    /// </summary>
    public async Task SendRequestSuccessAsync(string apiKeyHash, string model, string provider, int tokensUsed, decimal? cost, CancellationToken ct = default)
    {
        var webhookUrl = "http://localhost:9000/webhook"; // Placeholder URL - needs configuration
        await SendRequestSuccessWithUrlAsync(webhookUrl, Guid.NewGuid().ToString(), model, provider, tokensUsed, cost ?? 0, ct);
    }

    /// <summary>
    /// Отправляет webhook о превышении rate limit (для RateLimitEnforcerService)
    /// </summary>
    public async Task SendRateLimitExceededAsync(string apiKeyHash, string model, Core.Entities.RateLimitConfig? config, CancellationToken ct = default)
    {
        var webhookUrl = "http://localhost:9000/webhook"; // Placeholder URL - needs configuration
        var limitType = config?.RequestsPerMinute.HasValue == true ? "requests_per_minute" : "tokens_per_minute";
        var currentValue = 0;
        var limit = config?.RequestsPerMinute ?? 100;
        
        await SendRateLimitExceededWithUrlAsync(webhookUrl, apiKeyHash, limitType, currentValue, limit, ct);
    }

    /// <summary>
    /// Отправляет webhook о превышении бюджета (для RateLimitEnforcerService)
    /// </summary>
    public async Task SendBudgetExceededAsync(string entityId, string entityType, Budget? budget, CancellationToken ct = default)
    {
        var webhookUrl = "http://localhost:9000/webhook"; // Placeholder URL - needs configuration
        await SendBudgetExceededWithUrlAsync(
            webhookUrl, 
            entityId, 
            entityType, 
            budget?.CurrentSpending ?? 0, 
            budget?.BudgetAmount ?? 0, 
            ct);
    }

    /// <summary>
    /// Отправляет webhook об ошибке запроса (для RateLimitEnforcerService)
    /// </summary>
    public async Task SendRequestErrorAsync(string apiKeyHash, string model, string error, string errorType, CancellationToken ct = default)
    {
        var webhookUrl = "http://localhost:9000/webhook"; // Placeholder URL - needs configuration
        await SendRequestErrorWithUrlAsync(webhookUrl, Guid.NewGuid().ToString(), model, error, ct);
    }
}

/// <summary>
/// Типы webhook событий
/// </summary>
public enum WebhookEventType
{
    RequestSuccess,
    RequestError,
    RateLimitExceeded,
    BudgetExceeded,
    TeamMemberAdded,
    TeamMemberRemoved,
    BudgetUpdated
}

/// <summary>
/// Интерфейс для webhook сервиса
/// </summary>
public interface IWebhookService
{
    Task<bool> SendWebhookAsync(string webhookUrl, WebhookEventType eventType, object data, CancellationToken ct = default);
    Task<bool> SendRequestSuccessWithUrlAsync(string webhookUrl, string requestId, string model, string provider, int tokensUsed, decimal cost, CancellationToken ct = default);
    Task<bool> SendRequestErrorWithUrlAsync(string webhookUrl, string requestId, string model, string error, CancellationToken ct = default);
    Task<bool> SendRateLimitExceededWithUrlAsync(string webhookUrl, string apiKeyHash, string limitType, int currentValue, int limit, CancellationToken ct = default);
    Task<bool> SendBudgetExceededWithUrlAsync(string webhookUrl, string entityId, string entityType, decimal currentSpending, decimal budgetAmount, CancellationToken ct = default);
    
    // Методы для RateLimitEnforcerService (без webhookUrl параметра)
    Task SendRequestSuccessAsync(string apiKeyHash, string model, string provider, int tokensUsed, decimal? cost, CancellationToken ct = default);
    Task SendRequestErrorAsync(string apiKeyHash, string model, string error, string errorType, CancellationToken ct = default);
    Task SendRateLimitExceededAsync(string apiKeyHash, string model, RateLimitConfig? config, CancellationToken ct = default);
    Task SendBudgetExceededAsync(string entityId, string entityType, Budget? budget, CancellationToken ct = default);
}