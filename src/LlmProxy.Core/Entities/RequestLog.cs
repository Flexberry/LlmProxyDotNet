namespace LlmProxy.Core.Entities;

public class RequestLog
{
    public Guid Id { get; set; }
    public string ApiKeyHash { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string ModelRequested { get; set; } = string.Empty;
    public string ModelUsed { get; set; } = string.Empty;
    public int LatencyMs { get; set; }
    public int? TokensPrompt { get; set; }
    public int? TokensCompletion { get; set; }
    public string Status { get; set; } = "pending";
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}