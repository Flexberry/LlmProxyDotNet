using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using LlmProxy.Core.Config;
using LlmProxy.Core.Models.Dto;
using LlmProxy.Core.Providers;
using Microsoft.Extensions.Logging;

namespace LlmProxy.Infrastructure.Providers.OpenAI;

public class OpenAIAdapter : BaseHttpAdapter, ILlmProvider
{
    public string ProviderName => "openai";
    public string Prefix => "openai/";

    public OpenAIAdapter(HttpClient httpClient, ProviderSettings settings, ILogger<OpenAIAdapter> logger) 
        : base(httpClient, settings, logger)
    {
        if (!string.IsNullOrEmpty(settings.ApiKey))
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
    }

    public async Task<ChatCompletionResponse> CreateChatCompletionAsync(
        ChatCompletionRequest request, CancellationToken ct = default)
    {
        // Убираем префикс модели перед отправкой в API
        var cleanRequest = request with { Model = StripPrefix(request.Model) };
        return await SendRequestAsync<ChatCompletionRequest, ChatCompletionResponse>(
            "/chat/completions", cleanRequest, ct);
    }

    public async IAsyncEnumerable<ChatCompletionChunk> CreateChatCompletionStreamAsync(
        ChatCompletionRequest request, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var cleanRequest = request with { Model = StripPrefix(request.Model), Stream = true };
        var json = JsonSerializer.Serialize(cleanRequest, BaseHttpAdapter.JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Исправлено: используем SendAsync вместо PostAsync для поддержки HttpCompletionOption
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/chat/completions") { Content = content };
        using var response = await _httpClient.SendAsync(httpRequest, 
            HttpCompletionOption.ResponseHeadersRead, ct);
        
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;
            
            if (line.StartsWith("data: ", StringComparison.Ordinal))
            {
                var data = line["data: ".Length..].Trim();
                if (data == "[DONE]") break;
                
                var chunk = JsonSerializer.Deserialize<ChatCompletionChunk>(data, BaseHttpAdapter.JsonOptions);
                if (chunk != null) yield return chunk;
            }
        }
    }

    public async Task<EmbeddingResponse> CreateEmbeddingsAsync(
        EmbeddingRequest request, CancellationToken ct = default)
    {
        var cleanRequest = request with { Model = StripPrefix(request.Model) };
        return await SendRequestAsync<EmbeddingRequest, EmbeddingResponse>(
            "/embeddings", cleanRequest, ct);
    }

    private string StripPrefix(string model) => 
        model.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) 
            ? model[Prefix.Length..] 
            : model;
}