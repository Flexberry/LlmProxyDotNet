namespace LlmProxy.Core.Entities;

/// <summary>
/// Сущность API ключа для аутентификации клиентов
/// </summary>
public class ApiKey
{
    /// <summary>
    /// Уникальный идентификатор ключа
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// SHA256 хеш API ключа (хранится в БД вместо плоского ключа)
    /// </summary>
    public string KeyHash { get; set; } = string.Empty;

    /// <summary>
    /// Человеко-читаемое имя ключа для идентификации
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Разрешённые модели (JSON-массив названий моделей или "*" для всех)
    /// </summary>
    public string Permissions { get; set; } = "*";

    /// <summary>
    /// Дата и время истечения срока действия ключа (null = бессрочно)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Статус активности ключа (false = отозван)
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Дата и время создания ключа
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// ID команды (если ключ принадлежит команде)
    /// </summary>
    public Guid? TeamId { get; set; }

    /// <summary>
    /// Команда (навигационное свойство EF Core)
    /// </summary>
    public Team? Team { get; set; }

    /// <summary>
    /// Бюджет ключа (навигационное свойство EF Core)
    /// </summary>
    public Budget? Budget { get; set; }

    /// <summary>
    /// Конфигурация rate limits в формате JSON
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