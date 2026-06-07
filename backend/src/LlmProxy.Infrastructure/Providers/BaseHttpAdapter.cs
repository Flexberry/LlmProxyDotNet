using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LlmProxy.Core.Config;
using LlmProxy.Core.Models.Dto;

using Microsoft.Extensions.Logging;

namespace LlmProxy.Infrastructure.Providers;

/// <summary>
/// Базовый класс HTTP адаптера для провайдеров LLM
/// </summary>
public abstract class BaseHttpAdapter
{
    protected readonly HttpClient _httpClient;
    protected readonly ProviderSettings _settings;
    protected readonly ILogger _logger;

    /// <summary>
    /// Создаёт экземпляр базового HTTP адаптера
    /// </summary>
    /// <param name="httpClient">HTTP клиент для запросов</param>
    /// <param name="settings">Настройки провайдера</param>
    /// <param name="logger">Логгер для записи событий</param>
    protected BaseHttpAdapter(HttpClient httpClient, ProviderSettings settings, ILogger logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;
        
        if (!string.IsNullOrEmpty(settings.BaseUrl))
            _httpClient.BaseAddress = new Uri(settings.BaseUrl);
        
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "LlmProxyDotNet/1.0");
        
        // Добавляем API ключ, если он есть
        if (!string.IsNullOrEmpty(settings.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        }
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