using LlmProxy.Core.Models.Dto;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace LlmProxy.App.Controllers;

[ApiController]
[Route("v1/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ILogger<ChatController> _logger;

    public ChatController(ILogger<ChatController> logger)
    {
        _logger = logger;
    }

    [HttpPost("completions")]
    public async Task<IActionResult> CreateChatCompletion([FromBody] ChatCompletionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Model))
            return BadRequest(new { error = "Missing required field: model" });
        
        if (request.Messages == null || !request.Messages.Any())
            return BadRequest(new { error = "Missing required field: messages" });

        // TODO: Интеграция с Router и Provider (Этап 5-6)
        // Пока используем мок-данные

        var mockId = $"chatcmpl-{Guid.NewGuid():N}";
        
        if (request.Stream == true)
        {
            return await StreamMockResponse(mockId, request.Model);
        }

        var mockResponse = new ChatCompletionResponse
        {
            Id = mockId,
            Model = request.Model,
            Choices = new List<ChatChoice>
            {
                new()
                {
                    Index = 0,
                    Message = new ChatMessage { Role = "assistant", Content = "This is a mock response." },
                    FinishReason = "stop"
                }
            },
            Usage = new Usage { PromptTokens = 10, CompletionTokens = 15, TotalTokens = 25 }
        };

        return Ok(mockResponse);
    }

    private async Task<IActionResult> StreamMockResponse(string id, string model)
    {
        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        
        // Важно: отключаем буферизацию ответа, чтобы чанки уходили сразу
        // В .NET 7+ это часто работает по умолчанию для text/event-stream, 
        // но явная запись в BodyWriter надежнее.

        var chunks = GenerateMockChunks(id, model);
        
        await foreach (var chunk in chunks)
        {
            var json = JsonSerializer.Serialize(chunk, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull 
            });
            
            var data = $"data: {json}\n\n";
            var bytes = Encoding.UTF8.GetBytes(data);
            
            await Response.Body.WriteAsync(bytes);
            await Response.Body.FlushAsync();
        }

        // Отправляем финальный маркер [DONE]
        var doneBytes = Encoding.UTF8.GetBytes("data: [DONE]\n\n");
        await Response.Body.WriteAsync(doneBytes);
        await Response.Body.FlushAsync();

        return new EmptyResult();
    }

    private async IAsyncEnumerable<ChatCompletionChunk> GenerateMockChunks(string id, string model)
    {
        // Yield initial chunk with role
        yield return new ChatCompletionChunk
        {
            Id = id,
            Model = model,
            Choices = new List<ChatChunkChoice>
            {
                new() { Index = 0, Delta = new ChatMessage { Role = "assistant" } }
            }
        };

        // Simulate streaming content word by word
        var content = "Hello from LlmProxyDotNet! This is a simulated stream.";
        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            yield return new ChatCompletionChunk
            {
                Id = id,
                Model = model,
                Choices = new List<ChatChunkChoice>
                {
                    new() { Index = 0, Delta = new ChatMessage { Content = word + " " } }
                }
            };
            
            // Имитация задержки сети
            await Task.Delay(50);
        }

        // Yield final chunk with finish_reason
        yield return new ChatCompletionChunk
        {
            Id = id,
            Model = model,
            Choices = new List<ChatChunkChoice>
            {
                new() { Index = 0, Delta = new ChatMessage(), FinishReason = "stop" }
            }
        };
    }
}