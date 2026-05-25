using LlmProxy.Core.Entities;
using LlmProxy.Core.Models.Dto;

namespace LlmProxy.Tests.Fixtures;

/// <summary>
/// Фабрика тестовых данных для упрощения написания тестов
/// </summary>
public static class TestDataFactory
{
    public static ApiKey CreateApiKey(string? keyHash = null, string permissions = "*", bool isActive = true, DateTime? expiresAt = null)
    {
        return new ApiKey
        {
            Id = Guid.NewGuid(),
            KeyHash = keyHash ?? $"hash_{Guid.NewGuid():N}",
            Permissions = permissions,
            IsActive = isActive,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static ChatCompletionRequest CreateChatRequest(string model, params (string role, string content)[] messages)
    {
        return new ChatCompletionRequest
        {
            Model = model,
            Messages = messages.Select(m => new ChatMessage { Role = m.role, Content = m.content }),
            Temperature = 0.7,
            MaxTokens = 100
        };
    }

    public static ChatCompletionResponse CreateChatResponse(string id, string model, string content, string finishReason = "stop")
    {
        return new ChatCompletionResponse
        {
            Id = id,
            Model = model,
            Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Object = "chat.completion",
            Choices = [
                new ChatChoice
                {
                    Index = 0,
                    Message = new ChatMessage { Role = "assistant", Content = content },
                    FinishReason = finishReason
                }
            ],
            Usage = new Usage { PromptTokens = 10, CompletionTokens = content.Length / 4, TotalTokens = 10 + content.Length / 4 }
        };
    }

    public static ChatCompletionChunk CreateChatChunk(string id, string model, string? deltaContent = null, string? finishReason = null)
    {
        return new ChatCompletionChunk
        {
            Id = id,
            Model = model,
            Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Object = "chat.completion.chunk",
            Choices = [
                new ChatChunkChoice
                {
                    Index = 0,
                    Delta = new ChatMessage { Content = deltaContent },
                    FinishReason = finishReason
                }
            ]
        };
    }

    public static EmbeddingRequest CreateEmbeddingRequest(string model, object input)
    {
        return new EmbeddingRequest
        {
            Model = model,
            Input = input,
            EncodingFormat = "float"
        };
    }

    public static EmbeddingResponse CreateEmbeddingResponse(string model, int dimensions = 1536)
    {
        var embedding = Enumerable.Repeat(0.1f, dimensions).ToList();
        return new EmbeddingResponse
        {
            Model = model,
            Object = "list",
            Data = [
                new EmbeddingData
                {
                    Index = 0,
                    Object = "embedding",
                    Embedding = embedding
                }
            ],
            Usage = new Usage { PromptTokens = 5, CompletionTokens = 0, TotalTokens = 5 }
        };
    }
}