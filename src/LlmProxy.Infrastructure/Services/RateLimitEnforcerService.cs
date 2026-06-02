using LlmProxy.Core.Entities;
using Microsoft.Extensions.Logging;

namespace LlmProxy.Infrastructure.Services;

/// <summary>
/// Сервис для принудительного применения rate limiting и бюджетов в основном потоке запроса
/// </summary>
public class RateLimitEnforcerService : IRateLimitEnforcerService
{
    private readonly IRateLimitService _rateLimitService;
    private readonly IBudgetService _budgetService;
    private readonly IWebhookService _webhookService;
    private readonly ILogger<RateLimitEnforcerService> _logger;

    public RateLimitEnforcerService(
        IRateLimitService rateLimitService,
        IBudgetService budgetService,
        IWebhookService webhookService,
        ILogger<RateLimitEnforcerService> logger)
    {
        _rateLimitService = rateLimitService;
        _budgetService = budgetService;
        _webhookService = webhookService;
        _logger = logger;
    }

    /// <summary>
    /// Проверяет rate limits и бюджет перед выполнением запроса
    /// </summary>
    public async Task<EnforcementResult> CheckAndEnforceAsync(
        ApiKey apiKey,
        string modelName,
        CancellationToken ct = default)
    {
        var result = new EnforcementResult { IsAllowed = true };

        // 1. Проверка Rate Limits
        if (!string.IsNullOrEmpty(apiKey.RateLimitConfigJson))
        {
            var rateLimitConfig = apiKey.RateLimitConfig;
            var rateLimitResult = await _rateLimitService.CheckRateLimitAsync(
                apiKey.KeyHash, rateLimitConfig, 0, ct);

            if (!rateLimitResult.IsAllowed)
            {
                _logger.LogWarning(
                    "Rate limit exceeded for API key {ApiKeyHash}. Requests: {RequestsThisMinute}/{MaxPerMinute}",
                    apiKey.KeyHash, rateLimitResult.RequestsThisMinute, rateLimitConfig?.RequestsPerMinute);

                result.IsAllowed = false;
                result.Reason = "Rate limit exceeded";
                result.RetryAfter = rateLimitResult.RetryAfter;

                // Отправка webhook
                await _webhookService.SendRateLimitExceededAsync(
                    apiKey.KeyHash, modelName, rateLimitConfig, ct);

                return result;
            }

            result.RequestsThisMinute = rateLimitResult.RequestsThisMinute;
            result.RequestsToday = rateLimitResult.RequestsToday;
        }

        // 2. Проверка бюджета (опционально, не блокирует если ошибка)
        try
        {
            var entityType = "ApiKey";
            var entityId = apiKey.Id.ToString();

            // Если ключ принадлежит команде, проверяем бюджет команды
            if (apiKey.TeamId.HasValue)
            {
                entityType = "Team";
                entityId = apiKey.TeamId.Value.ToString();
            }

            var budgetResult = await _budgetService.CheckBudgetAsync(entityId, entityType, ct);

            if (budgetResult.ShouldBlock)
            {
                _logger.LogWarning(
                    "Budget exceeded for {EntityType} {EntityId}. Spending: {CurrentSpending}/{BudgetAmount}",
                    entityType, entityId, budgetResult.Budget?.CurrentSpending, budgetResult.Budget?.BudgetAmount);

                result.IsAllowed = false;
                result.Reason = "Budget exceeded";

                // Отправка webhook
                await _webhookService.SendBudgetExceededAsync(
                    entityId, entityType, budgetResult.Budget, ct);

                return result;
            }

            result.RemainingBudget = budgetResult.RemainingBudget;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Budget check failed for API key {ApiKeyHash}, continuing without budget enforcement", apiKey.KeyHash);
            // Пропускаем ошибку бюджета, не блокируем запрос
        }

        return result;
    }

    /// <summary>
    /// Регистрирует успешный запрос (инкрементирует счетчики и обновляет бюджет)
    /// </summary>
    public async Task RecordSuccessAsync(
        ApiKey apiKey,
        string modelName,
        string providerName,
        int tokenCount,
        decimal? cost,
        CancellationToken ct = default)
    {
        try
        {
            // Инкремент rate limit счетчиков
            await _rateLimitService.IncrementRequestAsync(apiKey.KeyHash, tokenCount, ct);

            // Обновление бюджета
            if (cost.HasValue && cost.Value > 0)
            {
                var entityType = "ApiKey";
                var entityId = apiKey.Id.ToString();

                if (apiKey.TeamId.HasValue)
                {
                    entityType = "Team";
                    entityId = apiKey.TeamId.Value.ToString();
                }

                await _budgetService.UpdateSpendingAsync(entityId, entityType, cost.Value, ct);
            }

            // Отправка webhook для успешного запроса
            await _webhookService.SendRequestSuccessAsync(
                apiKey.KeyHash,
                modelName,
                providerName,
                tokenCount,
                cost,
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording success for API key {ApiKeyHash}", apiKey.KeyHash);
            // Не блокируем запрос, если ошибка записи
        }
    }

    /// <summary>
    /// Регистрирует ошибку запроса
    /// </summary>
    public async Task RecordErrorAsync(
        ApiKey apiKey,
        string modelName,
        Exception error,
        CancellationToken ct = default)
    {
        try
        {
            await _webhookService.SendRequestErrorAsync(
                apiKey.KeyHash,
                modelName,
                error.Message,
                error.GetType().Name,
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording error for API key {ApiKeyHash}", apiKey.KeyHash);
        }
    }
}

/// <summary>
/// Результат проверки enforcement
/// </summary>
public class EnforcementResult
{
    public bool IsAllowed { get; set; }
    public string? Reason { get; set; }
    public TimeSpan? RetryAfter { get; set; }
    public int RequestsThisMinute { get; set; }
    public int RequestsToday { get; set; }
    public decimal RemainingBudget { get; set; }
}

/// <summary>
/// Интерфейс для сервиса принудительного применения лимитов
/// </summary>
public interface IRateLimitEnforcerService
{
    Task<EnforcementResult> CheckAndEnforceAsync(ApiKey apiKey, string modelName, CancellationToken ct = default);
    Task RecordSuccessAsync(ApiKey apiKey, string modelName, string providerName, int tokenCount, decimal? cost, CancellationToken ct = default);
    Task RecordErrorAsync(ApiKey apiKey, string modelName, Exception error, CancellationToken ct = default);
}