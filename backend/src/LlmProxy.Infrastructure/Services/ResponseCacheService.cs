using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Linq;

namespace LlmProxy.Infrastructure.Services;

/// <summary>
/// Сервис кэширования ответов LLM
/// </summary>
public class ResponseCacheService : IResponseCacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<ResponseCacheService> _logger;
    private readonly TimeSpan _defaultCacheTtl = TimeSpan.FromHours(24);

    public ResponseCacheService(IConnectionMultiplexer redis, ILogger<ResponseCacheService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<string?> GetCachedResponseAsync(string cacheKey, CancellationToken ct = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync(cacheKey);
            
            if (value.HasValue)
            {
                _logger.LogDebug("Cache hit for key: {CacheKey}", cacheKey);
                return value!;
            }
            
            _logger.LogDebug("Cache miss for key: {CacheKey}", cacheKey);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading from cache: {CacheKey}", cacheKey);
            return null;
        }
    }

    public async Task SetCachedResponseAsync(string cacheKey, string response, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.StringSetAsync(cacheKey, response, ttl ?? _defaultCacheTtl);
            _logger.LogDebug("Cached response for key: {CacheKey}", cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error writing to cache: {CacheKey}", cacheKey);
        }
    }

    public string GenerateCacheKey(string model, string prompt, Dictionary<string, object>? parameters = null)
    {
        var keyBuilder = new System.Text.StringBuilder();
        keyBuilder.Append($"llm:{model}:");
        keyBuilder.Append(ComputeHash(prompt));
        
        if (parameters != null && parameters.Any())
        {
            var sortedParams = parameters.OrderBy(p => p.Key);
            foreach (var param in sortedParams)
            {
                keyBuilder.Append($":{param.Key}={param.Value}");
            }
        }
        
        return keyBuilder.ToString();
    }

    public async Task ClearModelCacheAsync(string model, CancellationToken ct = default)
    {
        try
        {
            // Note: Full key scanning is not supported in StackExchange.Redis v2.x
            // This is a placeholder for future implementation
            _logger.LogInformation("Cache clearing for model: {Model} - manual cleanup required", model);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error clearing cache for model: {Model}", model);
        }
    }

    public async Task ClearAllCacheAsync(CancellationToken ct = default)
    {
        try
        {
            // Note: Full key scanning is not supported in StackExchange.Redis v2.x
            // This is a placeholder for future implementation
            _logger.LogInformation("Clear all cache - manual cleanup required");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error clearing all cache");
        }
    }

    private static string ComputeHash(string input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
}

public interface IResponseCacheService
{
    Task<string?> GetCachedResponseAsync(string cacheKey, CancellationToken ct = default);
    Task SetCachedResponseAsync(string cacheKey, string response, TimeSpan? ttl = null, CancellationToken ct = default);
    string GenerateCacheKey(string model, string prompt, Dictionary<string, object>? parameters = null);
    Task ClearModelCacheAsync(string model, CancellationToken ct = default);
    Task ClearAllCacheAsync(CancellationToken ct = default);
}