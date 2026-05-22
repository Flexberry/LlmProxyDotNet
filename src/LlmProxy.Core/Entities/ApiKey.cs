namespace LlmProxy.Core.Entities;
public class ApiKey
{
    public Guid Id { get; set; }
    public string KeyHash { get; set; } = string.Empty; // SHA256
    public string? Name { get; set; }
    public string Permissions { get; set; } = "*";      // JSON-массив или "*"
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}