using System.Text.Json.Serialization;

namespace LlmProxy.Core.Models.Dto;

public record EmbeddingRequest
{
    [JsonPropertyName("model")]
    public string Model { get; init; } = string.Empty;
    
    [JsonPropertyName("input")]
    public object Input { get; init; } = string.Empty; // string или string[]
    
    [JsonPropertyName("encoding_format")]
    public string? EncodingFormat { get; init; } // "float" или "base64"
    
    [JsonPropertyName("dimensions")]
    public int? Dimensions { get; init; }
    
    [JsonPropertyName("user")]
    public string? User { get; init; }
}