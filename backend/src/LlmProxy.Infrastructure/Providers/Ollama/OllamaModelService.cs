using System.Text.Json;
using System.Text.Json.Serialization;
using LlmProxy.Core.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LlmProxy.Infrastructure.Providers.Ollama;

/// <summary>
/// Сервис для получения списка моделей из Ollama
/// </summary>
public class OllamaModelService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaModelService> _logger;
    private List<string>? _cachedModels;
    private DateTime _cacheTimestamp;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

    public OllamaModelService(
        IOptions<LlmConfig> config,
        HttpClient httpClient,
        ILogger<OllamaModelService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Настройка BaseUrl из конфига
        var ollamaConfig = config.Value.Providers.TryGetValue("ollama", out var settings) 
            ? settings.BaseUrl 
            : "http://localhost:11434";
        
        _httpClient.BaseAddress = new Uri(ollamaConfig);
    }

    /// <summary>
    /// Получает список доступных моделей из Ollama
    /// </summary>
    /// <remarks>
    /// Примечание: Ollama API /api/tags может не возвращать все установленные модели.
    /// Поэтому мы возвращаем пустой список и полагаемся на прямую проверку моделей при запросе.
    /// </remarks>
    public async Task<IEnumerable<string>> GetAvailableModelsAsync(CancellationToken ct = default)
    {
        // Простая кэшированная реализация
        if (_cachedModels != null && 
            DateTime.UtcNow - _cacheTimestamp < _cacheDuration)
        {
            return _cachedModels;
        }

        try
        {
            var response = await _httpClient.GetAsync("/api/tags", ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var tagsResponse = JsonSerializer.Deserialize<OllamaTagsResponse>(json);

            _cachedModels = tagsResponse?.Models?.Select(m => m.Name).ToList() 
                ?? new List<string>();

            _cacheTimestamp = DateTime.UtcNow;

            _logger.LogInformation("Retrieved {Count} models from Ollama API", _cachedModels.Count);
            return _cachedModels;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch models from Ollama API");
            // Возвращаем пустой список - модель будет проверена при запросе
            _cachedModels = new List<string>();
            _cacheTimestamp = DateTime.UtcNow;
            return _cachedModels;
        }
    }

    /// <summary>
    /// Проверяет, существует ли модель в Ollama путем попытки запроса
    /// </summary>
    public async Task<bool> ModelExistsAsync(string modelName, CancellationToken ct = default)
    {
        // Сначала проверяем кэш /api/tags
        var cachedModels = await GetAvailableModelsAsync(ct);
        if (cachedModels.Any(m => 
            m.Equals(modelName, StringComparison.OrdinalIgnoreCase) ||
            m.Equals($"{modelName}:latest", StringComparison.OrdinalIgnoreCase) ||
            m.StartsWith($"{modelName}:", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // Если модель не найдена в кэше, пробуем сделать тестовый запрос
        // Это workaround для случая, когда /api/tags не возвращает все модели
        try
        {
            var testRequest = new 
            {
                model = modelName,
                messages = new[] { new { role = "user", content = "x" } },
                stream = false,
                options = new { temperature = 0 }
            };
            var json = JsonSerializer.Serialize(testRequest);
            var content = new System.Net.Http.StringContent(
                json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/api/chat", content, ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

// Ollama API response for /api/tags
public record OllamaTagsResponse(
    [property: JsonPropertyName("models")] List<OllamaTagInfo>? Models
);

public record OllamaTagInfo(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("model")] string? Model,
    [property: JsonPropertyName("modified_at")] DateTime? ModifiedAt,
    [property: JsonPropertyName("size")] long? Size,
    [property: JsonPropertyName("digest")] string? Digest
);
