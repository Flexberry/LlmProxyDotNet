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

/// <summary>
/// Admin controller for managing API keys and retrieving statistics
/// </summary>
[ApiController]
[Route("admin")]
public class AdminController : ControllerBase
{
    private readonly IApiKeyStore _keyStore;
    private readonly LlmProxyDbContext _dbContext;
    private readonly ILogger<AdminController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminController"/> class
    /// </summary>
    /// <param name="keyStore">API key store service</param>
    /// <param name="dbContext">Database context</param>
    /// <param name="logger">Logger instance</param>
    public AdminController(IApiKeyStore keyStore, LlmProxyDbContext dbContext, ILogger<AdminController> logger)
    {
        _keyStore = keyStore ?? throw new ArgumentNullException(nameof(keyStore));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Lists all active API keys
    /// </summary>
    /// <returns>List of active API keys or error status code</returns>
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

    /// <summary>
    /// Creates a new API key with the specified permissions
    /// </summary>
    /// <param name="request">Request containing key details (name, permissions, expiration)</param>
    /// <returns>Created API key with plaintext key and metadata</returns>
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
                ExpiresAt = request.ExpiresAt?.ToUniversalTime(),
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

    /// <summary>
    /// Revokes an API key by its ID
    /// </summary>
    /// <param name="id">Unique identifier of the API key to revoke</param>
    /// <returns>No content on success, NotFound if key doesn't exist</returns>
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
    
    /// <summary>
    /// Retrieves statistics for requests within a date range
    /// </summary>
    /// <param name="from">Start date of the period (UTC)</param>
    /// <param name="to">End date of the period (UTC)</param>
    /// <returns>Statistics including total requests, success/error counts, latency, and breakdown by model/provider</returns>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        try 
        {
            if (_dbContext == null) 
                return StatusCode(500, new { error = "Database context is not initialized" });

            if (from > to)
                return BadRequest(new { error = "'from' date must be before 'to' date" });
            
            if ((to - from).TotalDays > 30)
                return BadRequest(new { error = "Date range cannot exceed 30 days" });

            var query = _dbContext.RequestLogs
                .AsNoTracking()
                .Where(l => l.CreatedAt >= from && l.CreatedAt <= to);

            var total = await query.CountAsync();
            var success = await query.CountAsync(l => l.Status == "success");
            var error = await query.CountAsync(l => l.Status == "error");
            
            var avgLatency = await query.AverageAsync(l => (double?)l.LatencyMs) ?? 0;

            var byModel = await query
                .GroupBy(l => l.ModelUsed)
                .Select(g => new { Model = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Model ?? "unknown", x => x.Count);

            var byProvider = await query
                .GroupBy(l => l.ProviderName)
                .Select(g => new { Provider = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Provider ?? "unknown", x => x.Count);

            _logger.LogInformation($"Stats retrieved: total={total}, success={success}, error={error}");

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
            _logger.LogError(ex, "Error fetching stats: {ErrorMessage}", ex.Message);
            return StatusCode(500, new { error = "Failed to fetch stats", details = ex.Message, stack = ex.StackTrace });
        }
    }
}

public record CreateKeyRequest(string? Name, string[]? Permissions, DateTime? ExpiresAt);