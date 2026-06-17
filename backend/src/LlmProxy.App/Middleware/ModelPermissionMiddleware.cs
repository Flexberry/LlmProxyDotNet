using LlmProxy.Core.Config;
using LlmProxy.Core.Entities;
using LlmProxy.Core.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace LlmProxy.App.Middleware;

public class ModelPermissionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Dictionary<string, string> _providerPrefixes;
    private readonly string? _defaultPrefix;
    private readonly ILogger _logger;

    public ModelPermissionMiddleware(
        RequestDelegate next,
        IOptions<LlmConfig> config,
        ILogger<ModelPermissionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        
        // Собираем маппинг: prefix -> provider_key
        _providerPrefixes = config.Value.Providers
            .Where(p => !string.IsNullOrEmpty(p.Value.Prefix))
            .ToDictionary(
                p => p.Value.Prefix!,
                p => p.Key,
                StringComparer.OrdinalIgnoreCase);

        // Сохраняем префикс default провайдера
        if (config.Value.Providers.TryGetValue(config.Value.DefaultProvider, out var defaultProvider)
            && !string.IsNullOrEmpty(defaultProvider.Prefix))
        {
            _defaultPrefix = defaultProvider.Prefix;
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Пропускаем, если не аутентифицированы или это master key
        if (context.Items["IsMaster"] is true || context.Items["ApiKey"] is not ApiKey apiKey)
        {
            await _next(context);
            return;
        }

        // Проверяем только эндпоинты, использующие модели
        if (context.Request.Path.StartsWithSegments("/v1/chat/completions") ||
            context.Request.Path.StartsWithSegments("/v1/embeddings"))
        {
        if (context.Request.Method == "POST")
            {
                // Ограничиваем размер тела для защиты от DoS (макс. 1MB)
                context.Request.EnableBuffering(bufferLimit: 1024 * 1024);
                
                using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;

                if (string.IsNullOrWhiteSpace(body))
                {
                    await _next(context);
                    return;
                }

                // Парсим модель из запроса с защитой от невалидного JSON
                var requestedModel = ExtractModelFromBody(body);
                
                if (!string.IsNullOrWhiteSpace(requestedModel))
                {
                    // Резолвим модель: если нет префикса, добавляем префикс первого провайдера
                    var resolvedModel = ResolveModel(requestedModel);

                    // Нормализуем модель (убираем :latest и другие теги) для проверки прав
                    var normalizedModel = NormalizeModelName(resolvedModel);
                    var normalizedRequested = NormalizeModelName(requestedModel);

                    var allowed = KeyHelper.HasModelPermission(apiKey.Permissions, resolvedModel) ||
                                  KeyHelper.HasModelPermission(apiKey.Permissions, requestedModel) ||
                                  KeyHelper.HasModelPermission(apiKey.Permissions, normalizedModel) ||
                                  KeyHelper.HasModelPermission(apiKey.Permissions, normalizedRequested);

                    if (!allowed)
                    {
                        _logger.LogWarning("Model permission denied for key {KeyHash}: model '{Model}' (resolved: '{Resolved}', normalized: '{Normalized}') not allowed", 
                            context.Items["ApiKeyHash"], requestedModel, resolvedModel, normalizedModel);
                        
                        context.Response.StatusCode = 403;
                        await context.Response.WriteAsJsonAsync(new { 
                            error = $"Model '{requestedModel}' is not allowed for this API key" 
                        });
                        return;
                    }
                }
            }
        }

        await _next(context);
    }

    /// <summary>
    /// Извлекает имя модели из JSON-тела запроса.
    /// </summary>
    private static string? ExtractModelFromBody(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty("model", out var modelProp)
                ? modelProp.GetString()
                : null;
        }
        catch
        {
            // Невалидный JSON — пропускаем проверку
            return null;
        }
    }

    /// <summary>
    /// Резолвит имя модели:
    /// - Если уже есть префикс (например "ollama/llama3.2") — возвращаем как есть
    /// - Если без префикса (например "llama3.2") — добавляем префикс из DefaultProvider
    /// </summary>
    private string ResolveModel(string modelName)
    {
        // Если модель уже с префиксом (содержит "/")
        if (modelName.Contains('/'))
            return modelName;

        // Иначе добавляем префикс из DefaultProvider
        if (!string.IsNullOrEmpty(_defaultPrefix))
        {
            return $"{_defaultPrefix}{modelName}";
        }

        return modelName;
    }

    /// <summary>
    /// Нормализует имя модели: убирает тег версии (например ":latest", ":v1.0").
    /// ollama/llama3.2:latest → ollama/llama3.2
    /// </summary>
    private static string NormalizeModelName(string modelName)
    {
        var colonIndex = modelName.LastIndexOf(':');
        if (colonIndex > 0)
        {
            return modelName[..colonIndex];
        }
        return modelName;
    }
}