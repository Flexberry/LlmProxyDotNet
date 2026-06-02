using LlmProxy.Core.Entities;
using LlmProxy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace LlmProxy.Infrastructure.Services;

/// <summary>
/// Сервис управления бюджетами
/// </summary>
public class BudgetService : IBudgetService
{
    private readonly LlmProxyDbContext _db;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<BudgetService> _logger;

    public BudgetService(
        LlmProxyDbContext db, 
        IConnectionMultiplexer redis,
        ILogger<BudgetService> logger)
    {
        _db = db;
        _redis = redis;
        _logger = logger;
    }

    public async Task<Budget> GetOrCreateBudgetAsync(
        string entityId, 
        string entityType, 
        decimal budgetAmount,
        CancellationToken ct = default)
    {
        var budget = await _db.Budgets
            .FirstOrDefaultAsync(b => b.EntityId == entityId && b.EntityType == entityType && b.IsActive, ct);
        
        if (budget == null)
        {
            budget = new Budget
            {
                EntityId = entityId,
                EntityType = entityType,
                BudgetAmount = budgetAmount,
                CurrentSpending = 0,
                PeriodStart = DateTime.UtcNow,
                IsActive = true,
                LimitAction = "warn"
            };
            
            _db.Budgets.Add(budget);
            await _db.SaveChangesAsync(ct);
        }
        
        return budget;
    }

    public async Task<BudgetCheckResult> UpdateSpendingAsync(
        string entityId, 
        string entityType, 
        decimal cost,
        CancellationToken ct = default)
    {
        var budget = await GetOrCreateBudgetAsync(entityId, entityType, 1000, ct);
        
        budget.CurrentSpending += cost;
        budget.UpdatedAt = DateTime.UtcNow;
        
        await _db.SaveChangesAsync(ct);
        await CacheBudgetAsync(budget, ct);
        
        return new BudgetCheckResult
        {
            Budget = budget,
            IsOverBudget = budget.CurrentSpending > budget.BudgetAmount,
            RemainingBudget = Math.Max(0, budget.BudgetAmount - budget.CurrentSpending),
            ShouldBlock = budget.CurrentSpending > budget.BudgetAmount && budget.LimitAction == "block"
        };
    }

    public async Task<BudgetCheckResult> CheckBudgetAsync(
        string entityId, 
        string entityType,
        CancellationToken ct = default)
    {
        var budget = await GetBudgetAsync(entityId, entityType, ct);
        
        if (budget == null)
        {
            return new BudgetCheckResult
            {
                Budget = null,
                IsOverBudget = false,
                RemainingBudget = decimal.MaxValue,
                ShouldBlock = false
            };
        }
        
        return new BudgetCheckResult
        {
            Budget = budget,
            IsOverBudget = budget.CurrentSpending > budget.BudgetAmount,
            RemainingBudget = Math.Max(0, budget.BudgetAmount - budget.CurrentSpending),
            ShouldBlock = budget.CurrentSpending > budget.BudgetAmount && budget.LimitAction == "block"
        };
    }

    public async Task<Budget> SetBudgetAsync(
        string entityId, 
        string entityType, 
        decimal amount,
        string limitAction = "warn",
        DateTime? periodEnd = null,
        CancellationToken ct = default)
    {
        var budget = await GetOrCreateBudgetAsync(entityId, entityType, amount, ct);
        
        budget.BudgetAmount = amount;
        budget.LimitAction = limitAction;
        budget.PeriodEnd = periodEnd;
        budget.UpdatedAt = DateTime.UtcNow;
        
        await _db.SaveChangesAsync(ct);
        
        return budget;
    }

    public async Task<Budget?> GetBudgetAsync(
        string entityId, 
        string entityType,
        CancellationToken ct = default)
    {
        var cached = await GetBudgetFromCacheAsync(entityId, entityType, ct);
        if (cached != null) return cached;
        
        var budget = await _db.Budgets
            .FirstOrDefaultAsync(b => b.EntityId == entityId && b.EntityType == entityType && b.IsActive, ct);
        
        if (budget != null)
        {
            await CacheBudgetAsync(budget, ct);
        }
        
        return budget;
    }

    private async Task<Budget?> GetBudgetFromCacheAsync(string entityId, string entityType, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var key = $"budget:{entityId}:{entityType}";
        
        var cached = await db.StringGetAsync(key);
        if (cached.HasValue)
        {
            return System.Text.Json.JsonSerializer.Deserialize<Budget>(cached!.ToString());
        }
        
        return null;
    }

    private async Task CacheBudgetAsync(Budget budget, CancellationToken ct)
    {
        var db = _redis.GetDatabase();
        var key = $"budget:{budget.EntityId}:{budget.EntityType}";
        
        await db.StringSetAsync(key, System.Text.Json.JsonSerializer.Serialize(budget), TimeSpan.FromHours(1));
    }
}

public class BudgetCheckResult
{
    public Budget? Budget { get; set; }
    public bool IsOverBudget { get; set; }
    public decimal RemainingBudget { get; set; }
    public bool ShouldBlock { get; set; }
}

public interface IBudgetService
{
    Task<Budget> GetOrCreateBudgetAsync(string entityId, string entityType, decimal budgetAmount, CancellationToken ct = default);
    Task<BudgetCheckResult> UpdateSpendingAsync(string entityId, string entityType, decimal cost, CancellationToken ct = default);
    Task<BudgetCheckResult> CheckBudgetAsync(string entityId, string entityType, CancellationToken ct = default);
    Task<Budget> SetBudgetAsync(string entityId, string entityType, decimal amount, string limitAction = "warn", DateTime? periodEnd = null, CancellationToken ct = default);
    Task<Budget?> GetBudgetAsync(string entityId, string entityType, CancellationToken ct = default);
}