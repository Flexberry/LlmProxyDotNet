namespace LlmProxy.Core.Entities;

/// <summary>
/// Сущность API ключа
/// </summary>
public class ApiKey
{
    public Guid Id { get; set; }
    public string KeyHash { get; set; } = string.Empty; // SHA256
    public string? Name { get; set; }
    public string Permissions { get; set; } = "*";      // JSON-массив или "*"
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// ID команды (если ключ принадлежит команде)
    /// </summary>
    public Guid? TeamId { get; set; }
    
    /// <summary>
    /// Команда (навигационное свойство)
    /// </summary>
    public Team? Team { get; set; }
    
    /// <summary>
    /// Бюджет ключа
    /// </summary>
    public Budget? Budget { get; set; }
    
    /// <summary>
    /// Конфигурация rate limits (JSON)
    /// </summary>
    public string? RateLimitConfigJson { get; set; }
    
    /// <summary>
    /// Десериализованная конфигурация rate limits
    /// </summary>
    public RateLimitConfig? RateLimitConfig => 
        string.IsNullOrEmpty(RateLimitConfigJson) 
            ? null 
            : System.Text.Json.JsonSerializer.Deserialize<RateLimitConfig>(RateLimitConfigJson);
}