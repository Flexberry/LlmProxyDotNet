// src/LlmProxy.App/Controllers/AdminController.cs
using LlmProxy.Core.Auth;
using LlmProxy.Core.Entities;
using LlmProxy.Core.Utils;
using LlmProxy.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LlmProxy.App.Controllers;

[ApiController]
[Route("admin")]
public class AdminController : ControllerBase
{
    private readonly IApiKeyStore _keyStore;
    private readonly LlmProxyDbContext _dbContext;
    private readonly ILogger<AdminController> _logger;

    // Явно инжектим DbContext и Logger
    public AdminController(IApiKeyStore keyStore, LlmProxyDbContext dbContext, ILogger<AdminController> logger)
    {
        _keyStore = keyStore ?? throw new ArgumentNullException(nameof(keyStore));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet("keys")]
    public async Task<IActionResult> ListKeys()
    {
        try 
        {
            var keys = await _keyStore.ListActiveAsync(HttpContext.RequestAborted);
            return Ok(keys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching API keys");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [HttpPost("keys")]
    public async Task<IActionResult> CreateKey([FromBody] CreateKeyRequest request)
    {
        try 
        {
            var plaintextKey = KeyHelper.GenerateApiKey("sk");
            var hash = KeyHelper.HashKey(plaintextKey);

            var permissions = request.Permissions?.Length > 0 
                ? string.Join(",", request.Permissions) 
                : "*";

            var apiKey = new ApiKey
            {
                Id = Guid.NewGuid(),
                KeyHash = hash,
                Name = request.Name,
                Permissions = permissions,
                ExpiresAt = request.ExpiresAt,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _keyStore.CreateAsync(apiKey, HttpContext.RequestAborted);
            
            return Ok(new { Key = plaintextKey, ApiKey = apiKey });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating API key");
            return StatusCode(500, new { error = "Failed to create key", details = ex.Message });
        }
    }

    [HttpDelete("keys/{id}")]
    public async Task<IActionResult> RevokeKey(Guid id)
    {
        try 
        {
            var success = await _keyStore.RevokeAsync(id, HttpContext.RequestAborted);
            if (!success) return NotFound(new { error = "Key not found" });
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking key {KeyId}", id);
            return StatusCode(500, new { error = "Failed to revoke key", details = ex.Message });
        }
    }
    
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        try 
        {
            // Защита от NullReferenceException
            if (_dbContext == null) 
                return StatusCode(500, new { error = "Database context is not initialized" });

            var query = _dbContext.RequestLogs
                .AsNoTracking()
                .Where(l => l.CreatedAt >= from && l.CreatedAt <= to);

            var total = await query.CountAsync();
            var success = await query.CountAsync(l => l.Status == "success");
            var error = await query.CountAsync(l => l.Status == "error");
            
            // Безопасное получение среднего значения (если логов нет, Average вернет null)
            var avgLatency = await query.AverageAsync(l => (double?)l.LatencyMs) ?? 0;

            // Группировка по моделям
            var byModel = await query
                .GroupBy(l => l.ModelUsed)
                .Select(g => new { Model = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Model ?? "unknown", x => x.Count);

            // Группировка по провайдерам
            var byProvider = await query
                .GroupBy(l => l.ProviderName)
                .Select(g => new { Provider = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Provider ?? "unknown", x => x.Count);

            return Ok(new {
                totalRequests = total,
                successCount = success,
                errorCount = error,
                avgLatencyMs = Math.Round(avgLatency, 2),
                requestsByModel = byModel,
                requestsByProvider = byProvider
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching stats");
            return StatusCode(500, new { error = "Failed to fetch stats", details = ex.Message });
        }
    }
}

public record CreateKeyRequest(string? Name, string[]? Permissions, DateTime? ExpiresAt);