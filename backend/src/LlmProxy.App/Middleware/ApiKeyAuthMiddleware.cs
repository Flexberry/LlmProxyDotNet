using LlmProxy.Core.Auth;
using LlmProxy.Core.Utils;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace LlmProxy.App.Middleware;

public class ApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _masterKey;

    public ApiKeyAuthMiddleware(RequestDelegate next, IConfiguration config)
    {
        _next = next;
        _masterKey = config["LITELLM_MASTER_KEY"] ?? string.Empty;
    }

    public async Task InvokeAsync(HttpContext context, IApiKeyStore keyStore)
    {
        // 1. Пропускаем публичные эндпоинты
        if (IsPublicEndpoint(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // 2. Проверка Master Key (для админ-маршрутов и frontend)
        if (ValidateMasterKey(context))
        {
            context.Items["IsMaster"] = true;
            await _next(context);
            return;
        }

        // 3. Извлечение пользовательского ключа
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Missing or invalid Authorization header" });
            return;
        }

        var providedKey = authHeader["Bearer ".Length..].Trim();
        var keyHash = KeyHelper.HashKey(providedKey);

        // 4. Валидация ключа
        var apiKey = await keyStore.GetByKeyHashAsync(keyHash, context.RequestAborted);
        
        if (apiKey == null || !apiKey.IsActive)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid or revoked API key" });
            return;
        }

        if (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < DateTime.UtcNow)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "API key has expired" });
            return;
        }

        // 5. Сохраняем контекст ключа для дальнейшего использования
        context.Items["ApiKey"] = apiKey;
        context.Items["ApiKeyHash"] = keyHash;

        await _next(context);
    }

    private bool IsPublicEndpoint(PathString path)
    {
        // Публичные эндпоинты (без аутентификации)
        return path == "/health" || 
               path == "/v1/models" || 
               path.StartsWithSegments("/swagger");
    }

    private bool ValidateMasterKey(HttpContext context)
    {
        if (string.IsNullOrWhiteSpace(_masterKey)) return false;
        
        // Проверка через заголовок X-Admin-Key (для frontend)
        var adminKey = context.Request.Headers["X-Admin-Key"].FirstOrDefault();
        if (adminKey == _masterKey) return true;
        
        // Проверка через Bearer (для CLI-инструментов)
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        return authHeader == $"Bearer {_masterKey}";
    }
}