using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LlmProxy.Core.Config;
using LlmProxy.Core.Models.Dto;

using Microsoft.Extensions.Logging;

namespace LlmProxy.Infrastructure.Providers;

public abstract class BaseHttpAdapter
{
    protected readonly HttpClient _httpClient;
    protected readonly ProviderSettings _settings;
    protected readonly ILogger _logger;

    protected BaseHttpAdapter(HttpClient httpClient, ProviderSettings settings, ILogger logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;
        
        if (!string.IsNullOrEmpty(settings.BaseUrl))
            _httpClient.BaseAddress = new Uri(settings.BaseUrl);
        
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "LlmProxyDotNet/1.0");
    }

    protected async Task<TResponse> SendRequestAsync<TRequest, TResponse>(
        string endpoint, 
        TRequest request, 
        CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync(endpoint, content, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Provider {Provider} error {Status}: {Content}", 
                _settings.Prefix, response.StatusCode, responseContent);
            throw new HttpRequestException($"Provider error: {response.StatusCode}", null, response.StatusCode);
        }
        
        return JsonSerializer.Deserialize<TResponse>(responseContent, JsonOptions) 
            ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };
}