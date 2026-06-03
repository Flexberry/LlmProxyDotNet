namespace LlmProxy.Core.Entities;

/// <summary>
/// Команда для многопользовательского доступа
/// </summary>
public class Team
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Название команды
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Описание
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// ID владельца (пользователя)
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;
    
    /// <summary>
    /// Активна ли команда
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Создана
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    ///API ключи команды
    /// </summary>
    public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
    
    /// <summary>
    /// Бюджет команды
    /// </summary>
    public Budget? Budget { get; set; }
}

/// <summary>
/// Участник команды
/// </summary>
public class TeamMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid TeamId { get; set; }
    
    public Team Team { get; set; } = null!;
    
    /// <summary>
    /// ID пользователя
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Роль (owner, admin, member, viewer)
    /// </summary>
    public string Role { get; set; } = "member";
    
    /// <summary>
    /// Разрешенные модели (JSON массив или "*" для всех)
    /// </summary>
    public string? AllowedModels { get; set; }
    
    /// <summary>
    /// Добавлен
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Роли в команде
/// </summary>
public static class TeamRoles
{
    public const string Owner = "owner";
    public const string Admin = "admin";
    public const string Member = "member";
    public const string Viewer = "viewer";
}