namespace LlmProxy.Core.Config;

/// <summary>
/// Основная конфигурация LLM Proxy с настройками всех провайдеров
/// </summary>
public record LlmConfig
{
    /// <summary>
    /// Провайдер по умолчанию, используемый если не указан явно
    /// </summary>
    public string DefaultProvider { get; set; } = "openai";

    /// <summary>
    /// Словарь настроек провайдеров, где ключ — имя провайдера
    /// </summary>
    public Dictionary<string, ProviderSettings> Providers { get; set; } = new();
    
    /// <summary>
    /// Настройки стратегии fallback при ошибках провайдеров
    /// </summary>
    public FallbackSettings Fallback { get; set; } = new();
}

/// <summary>
/// Настройки стратегии fallback при сбоях провайдеров
/// </summary>
public record FallbackSettings
{
    /// <summary>
    /// Максимальное количество попыток переключения на альтернативный провайдер
    /// </summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>
    /// Список провайдеров, для которых отключён fallback
    /// </summary>
    public List<string> IgnoreProviders { get; set; } = new();

    /// <summary>
    /// Включать ли fallback для потоковых ответов (streaming)
    /// </summary>
    /// <remarks>
    /// Отключён по умолчанию, так как fallback во время streaming сложен в реализации
    /// </remarks>
    public bool EnableForStreaming { get; set; } = false;
}