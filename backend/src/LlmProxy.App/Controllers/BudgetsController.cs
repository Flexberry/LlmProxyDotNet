using LlmProxy.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LlmProxy.App.Controllers;

[ApiController]
[Route("admin/[controller]")]
[Authorize]
public class BudgetsController : ControllerBase
{
    private readonly IBudgetService _budgetService;
    private readonly ILogger<BudgetsController> _logger;

    public BudgetsController(IBudgetService budgetService, ILogger<BudgetsController> logger)
    {
        _budgetService = budgetService;
        _logger = logger;
    }

    /// <summary>
    /// Получает бюджет для сущности
    /// </summary>
    [HttpGet("{entityType}/{entityId}")]
    public async Task<IActionResult> GetBudget(string entityType, string entityId, CancellationToken ct)
    {
        var budget = await _budgetService.GetBudgetAsync(entityId, entityType, ct);
        
        if (budget == null)
            return NotFound(new { error = "Budget not found" });
        
        return Ok(budget);
    }

    /// <summary>
    /// Устанавливает/обновляет бюджет
    /// </summary>
    [HttpPost("{entityType}/{entityId}")]
    public async Task<IActionResult> SetBudget(
        string entityType, 
        string entityId, 
        [FromBody] SetBudgetRequest request,
        CancellationToken ct)
    {
        try
        {
            var budget = await _budgetService.SetBudgetAsync(
                entityId, 
                entityType, 
                request.BudgetAmount,
                request.LimitAction ?? "warn",
                request.PeriodEnd,
                ct);
            
            return Ok(budget);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting budget for {EntityType}/{EntityId}", entityType, entityId);
            return StatusCode(500, new { error = "Failed to set budget" });
        }
    }

    /// <summary>
    /// Проверяет бюджет
    /// </summary>
    [HttpGet("{entityType}/{entityId}/check")]
    public async Task<IActionResult> CheckBudget(string entityType, string entityId, CancellationToken ct)
    {
        var result = await _budgetService.CheckBudgetAsync(entityId, entityType, ct);
        return Ok(result);
    }

    /// <summary>
    /// Обновляет расходы
    /// </summary>
    [HttpPost("{entityType}/{entityId}/spending")]
    public async Task<IActionResult> UpdateSpending(
        string entityType,
        string entityId,
        [FromBody] UpdateSpendingRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _budgetService.UpdateSpendingAsync(entityId, entityType, request.Cost, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating spending for {EntityType}/{EntityId}", entityType, entityId);
            return StatusCode(500, new { error = "Failed to update spending" });
        }
    }
}

public class SetBudgetRequest
{
    public decimal BudgetAmount { get; set; }
    public string? LimitAction { get; set; }
    public DateTime? PeriodEnd { get; set; }
}

public class UpdateSpendingRequest
{
    public decimal Cost { get; set; }
}