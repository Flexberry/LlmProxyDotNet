using LlmProxy.Core.Entities;
using LlmProxy.Core.Utils;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace LlmProxy.App.Middleware;

/// <summary>
/// HTTP Middleware для проверки прав доступа к моделям
/// </summary>
/// <remarks>
/// Проверяет, имеет ли аутентифицированный API ключ разрешение на использование
/// запрошенной модели. Работает только для эндпоинтов, использующих модели:
/// /v1/chat/completions и /v1/embeddings.
/// </remarks>
public class ModelPermissionMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Создаёт экземпляр middleware
    /// </summary>
    /// <param name="next">Следующий middleware в конвейере</param>
    public ModelPermissionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Обрабатывает HTTP запрос, проверяя права доступа к модели
    /// </summary>
    /// <param name="context">Контекст HTTP запроса</param>
    /// <returns>Асинхронная задача</returns>
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
            // Читаем тело запроса (только для POST, без нарушения stream)
            if (context.Request.Method == "POST")
            {
                context.Request.EnableBuffering();
                using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;

                // Парсим модель из запроса
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("model", out var modelProp))
                {
                    var requestedModel = modelProp.GetString();
                    
                    if (!string.IsNullOrWhiteSpace(requestedModel) && 
                        !KeyHelper.HasModelPermission(apiKey.Permissions, requestedModel))
                    {
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
}