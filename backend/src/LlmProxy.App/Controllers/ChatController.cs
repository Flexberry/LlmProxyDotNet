using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LlmProxy.Core.Entities;
using LlmProxy.Core.Logging;
using LlmProxy.Core.Models.Dto;
using LlmProxy.Core.Providers;
using LlmProxy.Core.Router;
using LlmProxy.Infrastructure.Providers;
using LlmProxy.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LlmProxy.App.Controllers;

/// <summary>
/// Контроллер для обработки запросов к чат-комплетшн endpoint
/// </summary>
[ApiController]
[Route("v1/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ILogger<ChatController> _logger;
    private readonly ILlmRouter _router;
    private readonly ProviderFactory _providerFactory;
    private readonly ILoggingService _loggingService;
    private readonly IRateLimitEnforcerService _enforcerService;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Создаёт экземпляр контроллера
    /// </summary>
    /// <param name="logger">Журналлер</param>
    /// <param name="router">Маршрутизатор провайдеров</param>
    /// <param name="providerFactory">Фабрика провайдеров</param>
    /// <param name="loggingService">Сервис логирования</param>
    /// <param name="enforcerService">Сервис enforcement rate limits и бюджетов</param>
    public ChatController(
        ILogger<ChatController> logger,
        ILlmRouter router,
        ProviderFactory providerFactory,
        ILoggingService loggingService,
        IRateLimitEnforcerService enforcerService)
    {
        _logger = logger;
        _router = router;
        _providerFactory = providerFactory;
        _loggingService = loggingService;
        _enforcerService = enforcerService;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// Создаёт завершение чата через LLM провайдера
    /// </summary>
    /// <param name="request">Запрос на создание завершения чата</param>
    /// <returns>Результат с ответом от провайдера</returns>
    [HttpPost("completions")]
    public async Task<IActionResult> CreateChatCompletion([FromBody] ChatCompletionRequest request)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        if (string.IsNullOrWhiteSpace(request.Model))
            return BadRequest(new { error = "Missing required field: model" });

        if (request.Messages == null || !request.Messages.Any())
            return BadRequest(new { error = "Missing required field: messages" });

        var apiKeyHash = HttpContext.Items["ApiKeyHash"] as string ?? "unknown";
        var apiKey = HttpContext.Items["ApiKey"] as ApiKey;

        // Проверка rate limits и бюджета перед обработкой запроса
        if (apiKey != null)
        {
            var enforcementResult = await _enforcerService.CheckAndEnforceAsync(
                apiKey, request.Model, HttpContext.RequestAborted);

            if (!enforcementResult.IsAllowed)
            {
                _logger.LogWarning(
                    "Request blocked by enforcement: {Reason} for API key {ApiKeyHash}",
                    enforcementResult.Reason, apiKeyHash);

                return StatusCode(429, new
                {
                    error = enforcementResult.Reason,
                    retryAfter = enforcementResult.RetryAfter?.TotalSeconds ?? 60
                });
            }
        }

        try
        {
            var provider = await _router.SelectProviderAsync(
                request.Model, _providerFactory.GetAll(), HttpContext.RequestAborted);

            if (request.Stream == true)
            {
                return await StreamWithProvider(request, provider, apiKey, apiKeyHash, stopwatch);
            }
            else
            {
                var response = await _router.ExecuteWithFallback(
                    (p, ct) => p.CreateChatCompletionAsync(request, ct),
                    request.Model,
                    _providerFactory.GetAll(),
                    maxRetries: 2,
                    ct: HttpContext.RequestAborted);

                stopwatch.Stop();

                // Запись успешного запроса (инкремент счетчиков, обновление бюджета)
                if (apiKey != null)
                {
                    var tokensUsed = response.Usage?.TotalTokens ?? 0;
                    var cost = CalculateCost(provider.ProviderName, tokensUsed);
                    await _enforcerService.RecordSuccessAsync(
                        apiKey, request.Model, provider.ProviderName, tokensUsed, cost, HttpContext.RequestAborted);
                }

                _ = _loggingService.LogRequestAsync(
                    apiKeyHash,
                    provider.ProviderName,
                    request.Model,
                    response.Model,
                    (int)stopwatch.ElapsedMilliseconds,
                    "success",
                    response,
                    isStreaming: false);

                return Ok(response);
            }
        }
        catch (HttpRequestException httpEx)
        {
            stopwatch.Stop();
            _logger.LogWarning(httpEx, "HTTP error from provider");

            // Запись ошибки
            if (apiKey != null)
            {
                await _enforcerService.RecordErrorAsync(
                    apiKey, request.Model, httpEx, HttpContext.RequestAborted);
            }

            _ = _loggingService.LogRequestAsync(
                apiKeyHash,
                "unknown",
                request.Model,
                request.Model,
                (int)stopwatch.ElapsedMilliseconds,
                "error",
                error: httpEx);

            // Safe fallback for nullable StatusCode
            var statusCode = (int)(httpEx.StatusCode ?? HttpStatusCode.BadGateway);
            return StatusCode(statusCode, new { error = "Provider error", message = httpEx.Message });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error processing chat completion");

            // Запись ошибки
            if (apiKey != null)
            {
                await _enforcerService.RecordErrorAsync(
                    apiKey, request.Model, ex, HttpContext.RequestAborted);
            }

            _ = _loggingService.LogRequestAsync(
                apiKeyHash,
                "unknown",
                request.Model,
                request.Model,
                (int)stopwatch.ElapsedMilliseconds,
                "error",
                error: ex);

            return StatusCode(502, new { error = "Bad Gateway", message = ex.Message });
        }
    }

    /// <summary>
    /// Создаёт потоковый ответ от провайдера
    /// </summary>
    private async Task<IActionResult> StreamWithProvider(
        ChatCompletionRequest request,
        ILlmProvider provider,
        ApiKey? apiKey,
        string apiKeyHash,
        System.Diagnostics.Stopwatch stopwatch)
    {
        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";

        try
        {
            if (provider == null)
            {
                return BadRequest(new { error = "Provider not found" });
            }

            int totalTokens = 0;
            await foreach (var chunk in provider.CreateChatCompletionStreamAsync(request, HttpContext.RequestAborted))
            {
                var json = JsonSerializer.Serialize(chunk, _jsonOptions);
                var data = $"data: {json}\n\n";
                var bytes = Encoding.UTF8.GetBytes(data);

                await Response.Body.WriteAsync(bytes, HttpContext.RequestAborted);
                await Response.Body.FlushAsync(HttpContext.RequestAborted);

                // Подсчет токенов из content
                if (chunk.Choices?.FirstOrDefault()?.Delta?.Content != null)
                {
                    var content = chunk.Choices.First().Delta?.Content;
                    if (content != null)
                    {
                        totalTokens += content.Length / 4; // Приблизительный подсчет
                    }
                }
            }

            var doneBytes = Encoding.UTF8.GetBytes("data: [DONE]\n\n");
            await Response.Body.WriteAsync(doneBytes, HttpContext.RequestAborted);

            stopwatch.Stop();

            // Запись успешного запроса для streaming
            if (apiKey != null)
            {
                var cost = CalculateCost(provider.ProviderName, totalTokens);
                await _enforcerService.RecordSuccessAsync(
                    apiKey, request.Model, provider.ProviderName, totalTokens, cost, HttpContext.RequestAborted);
            }

            _ = _loggingService.LogRequestAsync(
                apiKeyHash,
                provider.ProviderName,
                request.Model,
                request.Model,
                (int)stopwatch.ElapsedMilliseconds,
                "success",
                isStreaming: true);

            return new EmptyResult();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Запись ошибки для streaming
            if (apiKey != null)
            {
                await _enforcerService.RecordErrorAsync(
                    apiKey, request.Model, ex, HttpContext.RequestAborted);
            }

            _ = _loggingService.LogRequestAsync(
                apiKeyHash,
                provider?.ProviderName ?? "unknown",
                request.Model,
                request.Model,
                (int)stopwatch.ElapsedMilliseconds,
                "error",
                error: ex,
                isStreaming: true);
            throw;
        }
    }

    /// <summary>
    /// Приблизительный расчет стоимости запроса (в долларах)
    /// </summary>
    /// <param name="providerName">Имя провайдера</param>
    /// <param name="tokens">Количество токенов</param>
    /// <returns>Стоимость в долларах</returns>
    private static decimal CalculateCost(string providerName, int tokens)
    {
        // Примерные цены (могут быть настроены через конфигурацию)
        var prices = new Dictionary<string, decimal>
        {
            { "openai", 0.000002m },  // $2 за 1M токенов
            { "ollama", 0m },          // Бесплатно (локально)
            { "vllm", 0m },            // Бесплатно (локально)
            { "openrouter", 0.000001m },
            { "zai", 0.0000015m }
        };

        var pricePerToken = prices.GetValueOrDefault(providerName.ToLower(), 0.000001m);
        return tokens * pricePerToken;
    }
}