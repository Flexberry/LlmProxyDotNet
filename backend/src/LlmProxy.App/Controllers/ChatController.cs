// src/LlmProxy.App/Controllers/ChatController.cs
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
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LlmProxy.App.Controllers;

[ApiController]
[Route("v1/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ILogger<ChatController> _logger;
    private readonly ILlmRouter _router;
    private readonly ProviderFactory _providerFactory;
    private readonly ILoggingService _loggingService;  // <-- ДОБАВЛЕНО
    private readonly JsonSerializerOptions _jsonOptions;

    public ChatController(
        ILogger<ChatController> logger,
        ILlmRouter router,
        ProviderFactory providerFactory,
        ILoggingService loggingService)  // <-- ДОБАВЛЕНО В КОНСТРУКТОР
    {
        _logger = logger;
        _router = router;
        _providerFactory = providerFactory;
        _loggingService = loggingService;  // <-- ДОБАВЛЕНО
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    [HttpPost("completions")]
    public async Task<IActionResult> CreateChatCompletion([FromBody] ChatCompletionRequest request)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        if (string.IsNullOrWhiteSpace(request.Model))
            return BadRequest(new { error = "Missing required field: model" });
        
        if (request.Messages == null || !request.Messages.Any())
            return BadRequest(new { error = "Missing required field: messages" });

        var apiKeyHash = HttpContext.Items["ApiKeyHash"] as string ?? "unknown";
        
        try
        {
            var provider = await _router.SelectProviderAsync(
                request.Model, _providerFactory.GetAll(), HttpContext.RequestAborted);

            if (request.Stream == true)
            {
                return await StreamWithProvider(request, provider, apiKeyHash, stopwatch);
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

    private async Task<IActionResult> StreamWithProvider(
        ChatCompletionRequest request, 
        ILlmProvider provider, 
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

            await foreach (var chunk in provider.CreateChatCompletionStreamAsync(request, HttpContext.RequestAborted))
            {
                var json = JsonSerializer.Serialize(chunk, _jsonOptions);
                var data = $"data: {json}\n\n";
                var bytes = Encoding.UTF8.GetBytes(data);
                
                await Response.Body.WriteAsync(bytes, HttpContext.RequestAborted);
                await Response.Body.FlushAsync(HttpContext.RequestAborted);
            }

            var doneBytes = Encoding.UTF8.GetBytes("data: [DONE]\n\n");
            await Response.Body.WriteAsync(doneBytes, HttpContext.RequestAborted);

            stopwatch.Stop();

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
}