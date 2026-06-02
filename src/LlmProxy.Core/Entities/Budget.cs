namespace LlmProxy.Core.Entities;

/// <summary>
/// Бюджет для API ключа или команды
/// </summary>
public class Budget
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Ссылка на ApiKey или Team
    /// </summary>
    public string EntityId { get; set; } = string.Empty;
    
    /// <summary>
    /// Тип сущности (ApiKey, Team, Org)
    /// </summary>
    public string EntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// Максимальный бюджет (в долларах)
    /// </summary>
    public decimal BudgetAmount { get; set; }
    
    /// <summary>
    /// Текущие расходы
    /// </summary>
    public decimal CurrentSpending { get; set; }
    
    /// <summary>
    /// Период начала
    /// </summary>
    public DateTime PeriodStart { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Период конца
    /// </summary>
    public DateTime? PeriodEnd { get; set; }
    
    /// <summary>
    /// Активен ли бюджет
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Действие при достижении лимита (block, warn, continue)
    /// </summary>
    public string LimitAction { get; set; } = "warn";
    
    /// <summary>
    /// Создан
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Обновлено
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}