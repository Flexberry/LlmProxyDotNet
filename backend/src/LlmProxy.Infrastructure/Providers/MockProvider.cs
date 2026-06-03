using LlmProxy.Core.Config;
using LlmProxy.Core.Logging;
using LlmProxy.Core.Providers;
using LlmProxy.Core.Models.Dto;
using Microsoft.Extensions.Logging;

namespace LlmProxy.Infrastructure.Providers;

/// <summary>
/// Mock provider для тестов. Возвращает заглушки вместо реальных запросов.
/// </summary>
public class MockProvider : ILlmProvider
{
    private readonly ILogger<MockProvider> _logger;

    public string ProviderName => "mock";
    public string Prefix => "mock";

    public MockProvider(ILogger<MockProvider> logger)
    {
        _logger = logger;
    }

    public Task<ChatCompletionResponse> CreateChatCompletionAsync(
        ChatCompletionRequest request, 
        CancellationToken ct = default)
    {
        _logger.LogWarning("Mock provider called - returning mock response");
        
        var response = new ChatCompletionResponse
        {
            Id = "mock-123",
            Model = request.Model,
            Choices = new[]
            {
                new ChatChoice
                {
                    Index = 0,
                    Message = new ChatMessage
                    {
                        Role = "assistant",
                        Content = "This is a mock response"
                    },
                    FinishReason = "stop"
                }
            },
            Created = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
            Usage = new Usage { PromptTokens = 10, CompletionTokens = 5, TotalTokens = 15 }
        };

        return Task.FromResult(response);
    }

    public IAsyncEnumerable<ChatCompletionChunk> CreateChatCompletionStreamAsync(
        ChatCompletionRequest request, 
        CancellationToken ct = default)
    {
        _logger.LogWarning("Mock provider stream called");
        return MockStream();
        
        async IAsyncEnumerable<ChatCompletionChunk> MockStream()
        {
            yield return new ChatCompletionChunk
            {
                Id = "mock-123",
                Model = request.Model,
                Choices = new[]
                {
                    new ChatChunkChoice
                    {
                        Index = 0,
                        Delta = new ChatMessage { Role = "assistant", Content = "Mock stream response" },
                        FinishReason = "stop"
                    }
                },
                Created = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()
            };
        }
    }

    public Task<EmbeddingResponse> CreateEmbeddingsAsync(
        EmbeddingRequest request, 
        CancellationToken ct = default)
    {
        _logger.LogWarning("Mock provider embeddings called - returning mock response");
        
        var response = new EmbeddingResponse
        {
            Data = new[]
            {
                new EmbeddingData { Index = 0, Embedding = new float[10] }
            },
            Model = request.Model,
            Usage = new Usage { PromptTokens = 10, TotalTokens = 10 }
        };

        return Task.FromResult(response);
    }
}
