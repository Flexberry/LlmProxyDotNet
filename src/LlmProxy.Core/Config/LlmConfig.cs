namespace LlmProxy.Core.Config;
public record ProviderSettings(string BaseUrl, string? ApiKey, string Prefix);

public record LlmConfig
{
    public string DefaultProvider { get; set; } = "openai";
    public Dictionary<string, ProviderSettings> Providers { get; set; } = new();
}