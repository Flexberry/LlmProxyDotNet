using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LlmProxy.Core.Config;
using LlmProxy.Core.Models.Dto;
using LlmProxy.Core.Providers;
using Microsoft.Extensions.Logging;

namespace LlmProxy.Infrastructure.Providers.Ollama;

// Ollama-specific DTOs
public record OllamaChatRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("messages")] IEnumerable<OllamaMessage> Messages,
    [property: JsonPropertyName("stream")] bool Stream = false,
    [property: JsonPropertyName("options")] OllamaOptions? Options = null
);

public record OllamaMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content
);

public record OllamaOptions(
    [property: JsonPropertyName("temperature")] double? Temperature = null,
    [property: JsonPropertyName("num_predict")] int? MaxTokens = null
);

public record OllamaChatResponse(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("message")] OllamaMessage Message,
    [property: JsonPropertyName("done")] bool Done
);

public record OllamaStreamResponse(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("message")] OllamaMessage Message,
    [property: JsonPropertyName("done")] bool Done
);

public class OllamaAdapter : BaseHttpAdapter, ILlmProvider
{
    public string ProviderName => "ollama";
    public string Prefix => "ollama/";

    public OllamaAdapter(HttpClient httpClient, ProviderSettings settings, ILogger<OllamaAdapter> logger) 
        : base(httpClient, settings, logger) { }

    public async Task<ChatCompletionResponse> CreateChatCompletionAsync(
        ChatCompletionRequest request, CancellationToken ct = default)
    {
        var ollamaRequest = MapToOllamaRequest(request, stream: false);
        var response = await SendRequestAsync<OllamaChatRequest, OllamaChatResponse>(
            "/api/chat", ollamaRequest, ct);
        
        return MapToOpenAIResponse(response, request.Model);
    }

    public async IAsyncEnumerable<ChatCompletionChunk> CreateChatCompletionStreamAsync(
        ChatCompletionRequest request, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var ollamaRequest = MapToOllamaRequest(request, stream: true);
        var json = JsonSerializer.Serialize(ollamaRequest, BaseHttpAdapter.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/chat") { Content = content };
        using var response = await _httpClient.SendAsync(httpRequest, 
            HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;
            
            var chunk = JsonSerializer.Deserialize<OllamaStreamResponse>(line, BaseHttpAdapter.JsonOptions);
            if (chunk != null)
            {
                yield return MapToOpenAIChunk(chunk, request.Model, chunk.Done ? "stop" : null);
                if (chunk.Done) break;
            }
        }
    }

    public async Task<EmbeddingResponse> CreateEmbeddingsAsync(
        EmbeddingRequest request, CancellationToken ct = default)
    {
        // Ollama embeddings: POST /api/embed
        var embeddingRequest = new { model = StripPrefix(request.Model), input = request.Input };
        var json = JsonSerializer.Serialize(embeddingRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("/api/embed", content, ct);
        response.EnsureSuccessStatusCode();
        
        // Упрощённый маппинг (полная реализация требует парсинга Ollama embedding response)
        return new EmbeddingResponse
        {
            Model = request.Model,
            Data = Array.Empty<EmbeddingData>(),
            Usage = new Usage { PromptTokens = 0, CompletionTokens = 0, TotalTokens = 0 }
        };
    }

    private OllamaChatRequest MapToOllamaRequest(ChatCompletionRequest req, bool stream) => new(
        Model: StripPrefix(req.Model),
        Messages: req.Messages.Select(m => new OllamaMessage(m.Role, m.Content ?? string.Empty)),
        Stream: stream,
        Options: new OllamaOptions(req.Temperature, req.MaxTokens)
    );

    private ChatCompletionResponse MapToOpenAIResponse(OllamaChatResponse ollama, string originalModel) => new()
    {
        Id = $"ollama-{Guid.NewGuid():N}",
        Model = originalModel,
        Choices = new List<ChatChoice>
        {
            new() 
            { 
                Index = 0, 
                Message = new ChatMessage { Role = ollama.Message.Role, Content = ollama.Message.Content }, 
                FinishReason = "stop" 
            }
        },
        Usage = new Usage { PromptTokens = 0, CompletionTokens = 0, TotalTokens = 0 }
    };

    private ChatCompletionChunk MapToOpenAIChunk(OllamaStreamResponse ollama, string originalModel, string? finishReason) => new()
    {
        Id = $"ollama-{Guid.NewGuid():N}",
        Model = originalModel,
        Choices = new List<ChatChunkChoice>
        {
            new() 
            { 
                Index = 0, 
                Delta = new ChatMessage { Role = ollama.Message.Role, Content = ollama.Message.Content }, 
                FinishReason = finishReason 
            }
        }
    };

    private string StripPrefix(string model) => 
        model.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) 
            ? model[Prefix.Length..] 
            : model;
}