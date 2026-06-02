using LlmProxy.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LlmProxy.App.Controllers;

[ApiController]
[Route("admin/[controller]")]
[Authorize]
public class RateLimitsController : ControllerBase
{
    private readonly IRateLimitService _rateLimitService;
    private readonly ILogger<RateLimitsController> _logger;

    public RateLimitsController(IRateLimitService rateLimitService, ILogger<RateLimitsController> logger)
    {
        _rateLimitService = rateLimitService;
        _logger = logger;
    }

    /// <summary>
    /// Проверяет текущий статус лимитов для ключа
    /// </summary>
    [HttpGet("{apiKeyHash}")]
    public async Task<IActionResult> GetRateLimitStatus(string apiKeyHash, CancellationToken ct)
    {
        var result = await _rateLimitService.CheckRateLimitAsync(apiKeyHash, null, 0, ct);
        
        return Ok(new
        {
            apiKeyHash,
            isAllowed = result.IsAllowed,
            requestsThisMinute = result.RequestsThisMinute,
            requestsToday = result.RequestsToday,
            resetAt = result.ResetAt,
            retryAfter = result.RetryAfter.TotalSeconds
        });
    }

    /// <summary>
    /// Сбрасывает лимиты для ключа
    /// </summary>
    [HttpPost("{apiKeyHash}/reset")]
    public async Task<IActionResult> ResetLimits(string apiKeyHash, CancellationToken ct)
    {
        await _rateLimitService.ResetLimitsAsync(apiKeyHash, ct);
        return Ok(new { success = true, message = "Rate limits reset successfully" });
    }
}