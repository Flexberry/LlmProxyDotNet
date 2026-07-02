using System.Text.Json.Serialization;

namespace LlmProxy.Core.Models.Dto;

public record ChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; init; } = string.Empty; // "system", "user", "assistant", "tool"
    
    [JsonPropertyName("content")]
    public string? Content { get; init; }
    
    [JsonPropertyName("name")]
    public string? Name { get; init; }
    
    [JsonPropertyName("tool_calls")]
    public List<ToolCall>? ToolCalls { get; init; }
    
    [JsonPropertyName("tool_call_id")]
    public string? ToolCallId { get; init; }
}

public record ToolCall
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; init; } = "function";
    
    [JsonPropertyName("function")]
    public FunctionCall Function { get; init; } = new();
}

public record FunctionCall
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
    
    [JsonPropertyName("arguments")]
    public string Arguments { get; init; } = string.Empty;
}

public record ChatCompletionRequest
{
    [JsonPropertyName("model")]
    public string Model { get; init; } = string.Empty;
    
    [JsonPropertyName("messages")]
    public IEnumerable<ChatMessage> Messages { get; init; } = Enumerable.Empty<ChatMessage>();
    
    [JsonPropertyName("stream")]
    public bool? Stream { get; init; }
    
    [JsonPropertyName("temperature")]
    public double? Temperature { get; init; }
    
    [JsonPropertyName("top_p")]
    public double? TopP { get; init; }
    
    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; init; }
    
    [JsonPropertyName("stop")]
    public List<string>? Stop { get; init; }
    
    [JsonPropertyName("stream_options")]
    public StreamOptions? StreamOptions { get; init; }
}

public record StreamOptions
{
    [JsonPropertyName("include_usage")]
    public bool? IncludeUsage { get; init; }
}