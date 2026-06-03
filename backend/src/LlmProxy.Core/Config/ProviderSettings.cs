using LlmProxy.Core.Entities;

namespace LlmProxy.Core.Config;

/// <summary>
/// Настройки конфигурации для конкретного провайдера LLM
/// </summary>
public class ProviderSettings
{
    /// <summary>
    /// Базовый URL API провайдера (например, http://localhost:11434 для Ollama)
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// API ключ провайдера (для облачных провайдеров: OpenAI, OpenRouter, Z.ai)
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Префикс имени модели для маршрутизации (например, "ollama", "openai", "vllm")
    /// </summary>
    public string Prefix { get; set; } = string.Empty;
}

