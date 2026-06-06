namespace LlmProxy.Core.Models.Dto;
using System.Text.Json.Serialization;


public record ChatChoice
{
    [JsonPropertyName("index")]
    public int Index { get; init; }
    
    [JsonPropertyName("message")]
    public ChatMessage Message { get; init; } = new();
    
    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; init; } // "stop", "length", "tool_calls", "content_filter"
    
    [JsonPropertyName("logprobs")]
    public object? LogProbs { get; init; }
}

public record Usage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; init; }
    
    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; init; }
    
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; init; }
}

public record ChatCompletionResponse
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;
    
    [JsonPropertyName("object")]
    public string Object { get; init; } = "chat.completion";
    
    [JsonPropertyName("created")]
    public long Created { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    
    [JsonPropertyName("model")]
    public string Model { get; init; } = string.Empty;
    
    [JsonPropertyName("choices")]
    public IReadOnlyList<ChatChoice> Choices { get; init; } = Array.Empty<ChatChoice>();
    
    [JsonPropertyName("usage")]
    public Usage? Usage { get; init; }
    
    [JsonPropertyName("system_fingerprint")]
    public string? SystemFingerprint { get; init; }
}