namespace LlmProxy.Core.Config;

public class ProviderSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public string Prefix { get; set; } = string.Empty;
}

public record LlmConfig
{
    public string DefaultProvider { get; set; } = "openai";
    public Dictionary<string, ProviderSettings> Providers { get; set; } = new();
    
    // Настройки fallback (новые поля)
    public FallbackSettings Fallback { get; set; } = new();
}

public record FallbackSettings
{
    public int MaxRetries { get; set; } = 2;
    public List<string> IgnoreProviders { get; set; } = new();
    public bool EnableForStreaming { get; set; } = false; // Fallback сложен для streaming
}