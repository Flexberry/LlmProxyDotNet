namespace LlmProxy.Core.Models.Dto;

using System.Text.Json.Serialization;

public record ChatChunkChoice
{
    [JsonPropertyName("index")]
    public int Index { get; init; }
    
    [JsonPropertyName("delta")]
    public ChatMessage Delta { get; init; } = new();
    
    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; init; }
    
    [JsonPropertyName("logprobs")]
    public object? LogProbs { get; init; }
}

public record ChatCompletionChunk
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;
    
    [JsonPropertyName("object")]
    public string Object { get; init; } = "chat.completion.chunk";
    
    [JsonPropertyName("created")]
    public long Created { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    
    [JsonPropertyName("model")]
    public string Model { get; init; } = string.Empty;
    
    [JsonPropertyName("choices")]
    public IReadOnlyList<ChatChunkChoice> Choices { get; init; } = Array.Empty<ChatChunkChoice>();
    
    [JsonPropertyName("system_fingerprint")]
    public string? SystemFingerprint { get; init; }
    
    [JsonPropertyName("usage")]
    public Usage? Usage { get; init; }
}