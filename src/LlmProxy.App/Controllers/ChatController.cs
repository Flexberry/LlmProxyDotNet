using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LlmProxy.Core.Entities;
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
    private readonly JsonSerializerOptions _jsonOptions;

    // Конструктор с зависимостями
    public ChatController(
        ILogger<ChatController> logger,
        ILlmRouter router,
        ProviderFactory providerFactory)
    {
        _logger = logger;
        _router = router;
        _providerFactory = providerFactory;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    [HttpPost("completions")]
    public async Task<IActionResult> CreateChatCompletion([FromBody] ChatCompletionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Model))
            return BadRequest(new { error = "Missing required field: model" });
        
        if (request.Messages == null || !request.Messages.Any())
            return BadRequest(new { error = "Missing required field: messages" });

        var apiKey = HttpContext.Items["ApiKey"] as ApiKey;
        var apiKeyHash = HttpContext.Items["ApiKeyHash"] as string;

        try
        {
            var provider = await _router.SelectProviderAsync(
                request.Model, _providerFactory.GetAll(), HttpContext.RequestAborted);

            if (request.Stream == true)
            {
                return await StreamWithProvider(request, provider, apiKeyHash);
            }
            else
            {
                var response = await _router.ExecuteWithFallback(
                    (p, ct) => p.CreateChatCompletionAsync(request, ct),
                    request.Model,
                    _providerFactory.GetAll(),
                    maxRetries: 2,
                    ct: HttpContext.RequestAborted);
                
                _ = LogRequestAsync(apiKeyHash, request.Model, provider.ProviderName, response, null);
                
                return Ok(response);
            }
        }
        catch (AggregateException ex) when (ex.InnerException is HttpRequestException)
        {
            _logger.LogError(ex, "All providers failed for model {Model}", request.Model);
            return StatusCode(502, new { error = "All LLM providers are currently unavailable" });
        }
        // ИСПРАВЛЕНО: сравнение с enum HttpStatusCode, а не int
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            return StatusCode(401, new { error = "Invalid provider API key" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing chat completion");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private async Task<IActionResult> StreamWithProvider(
        ChatCompletionRequest request, 
        ILlmProvider provider, 
        string? apiKeyHash)
    {
        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";

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

        _ = LogRequestAsync(apiKeyHash, request.Model, provider.ProviderName, null, "streamed");

        return new EmptyResult();
    }

    // Заглушка для логирования (реализация в Этапе 7)
    private async Task LogRequestAsync(
        string? apiKeyHash, 
        string requestedModel, 
        string providerName, 
        ChatCompletionResponse? response, 
        string? status)
    {
        // TODO: Реализация асинхронной записи в RequestLog (Этап 7)
        await Task.Yield();
    }
}