using System.Text.Json.Serialization;

namespace LlmProxy.Core.Models.Dto;

public record EmbeddingData
{
    [JsonPropertyName("object")]
    public string Object { get; init; } = "embedding";
    
    [JsonPropertyName("index")]
    public int Index { get; init; }
    
    [JsonPropertyName("embedding")]
    public IReadOnlyList<float> Embedding { get; init; } = Array.Empty<float>();
}

public record EmbeddingResponse
{
    [JsonPropertyName("object")]
    public string Object { get; init; } = "list";
    
    [JsonPropertyName("data")]
    public IReadOnlyList<EmbeddingData> Data { get; init; } = Array.Empty<EmbeddingData>();
    
    [JsonPropertyName("model")]
    public string Model { get; init; } = string.Empty;
    
    [JsonPropertyName("usage")]
    public Usage Usage { get; init; } = new();
}