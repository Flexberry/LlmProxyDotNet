namespace LlmProxy.Core.Entities;

/// <summary>
/// Конфигурация лимитов для API ключа
/// </summary>
public class RateLimitConfig
{
    /// <summary>
    /// Максимальное количество запросов в минуту
    /// </summary>
    public int? RequestsPerMinute { get; set; }
    
    /// <summary>
    /// Максимальное количество токенов в минуту
    /// </summary>
    public int? TokensPerMinute { get; set; }
    
    /// <summary>
    /// Максимальное количество запросов в день
    /// </summary>
    public int? RequestsPerDay { get; set; }
    
    /// <summary>
    /// Максимальная стоимость в день (в долларах)
    /// </summary>
    public decimal? MaxDailyCost { get; set; }
}

/// <summary>
/// Информация о текущем использовании лимитов
/// </summary>
public class RateLimitStatus
{
    public int RequestsThisMinute { get; set; }
    public int TokensThisMinute { get; set; }
    public int RequestsToday { get; set; }
    public decimal DailyCost { get; set; }
    public DateTime ResetAt { get; set; }
    
    public bool IsRateLimited => 
        (RequestsThisMinute >= 100) || // default limit
        (RequestsToday >= 10000); // default daily limit
}