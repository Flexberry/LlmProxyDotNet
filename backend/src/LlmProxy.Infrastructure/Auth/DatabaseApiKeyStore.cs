using StackExchange.Redis;
using LlmProxy.Core.Auth;
using LlmProxy.Core.Entities;
using LlmProxy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LlmProxy.Infrastructure.Auth;

/// <summary>
/// API key store implementation with Redis caching support
/// </summary>
public class DatabaseApiKeyStore : IApiKeyStore
{
    private readonly LlmProxyDbContext _db;
    private readonly IConnectionMultiplexer _redis;
    private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(5);
    private const string CachePrefix = "api_key:";

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseApiKeyStore"/> class
    /// </summary>
    /// <param name="db">Database context</param>
    /// <param name="redis">Redis connection multiplexer</param>
    public DatabaseApiKeyStore(LlmProxyDbContext db, IConnectionMultiplexer redis)
    {
        _db = db;
        _redis = redis;
    }

    /// <inheritdoc/>
    /// <summary>
    /// Retrieves an API key by its hash
    /// </summary>
    /// <param name="keyHash">SHA256 hash of the API key</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>API key if found and active, null otherwise</returns>
    public async Task<ApiKey?> GetByKeyHashAsync(string keyHash, CancellationToken ct = default)
    {
        // Try to get from Redis cache
        ApiKey? cachedKey = null;
        bool cacheHit = false;
        try
        {
            var db = _redis.GetDatabase();
            var cached = await db.StringGetAsync($"{CachePrefix}{keyHash}");
            
            if (cached.HasValue)
            {
                var cachedString = cached.ToString();
                
                // If cache contains null marker
                if (cachedString != "null")
                {
                    cachedKey = JsonSerializer.Deserialize<ApiKey>(cachedString);
                    cacheHit = true;
                }
            }
        }
        catch (Exception ex)
        {
            // Redis unavailable - continue without cache
            Console.WriteLine($"Warning: Redis cache unavailable: {ex.Message}");
        }
        
        // If cache hit, CHECK IsActive in DB
        if (cacheHit && cachedKey != null)
        {
            var dbKey = await _db.ApiKeys
                .FirstOrDefaultAsync(k => k.KeyHash == keyHash, ct);
            
            // If key became inactive in DB, invalidate cache and return null
            if (dbKey == null || !dbKey.IsActive)
            {
                try
                {
                    var db = _redis.GetDatabase();
                    await db.KeyDeleteAsync($"{CachePrefix}{keyHash}");
                }
                catch { /* Ignore cache cleanup errors */ }
                return null;
            }
            
            return cachedKey;
        }
        
        // Query DB (fallback if Redis unavailable or cache miss)
        var key = await _db.ApiKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash && k.IsActive, ct);

        // Try to cache the result (no error if Redis unavailable)
        if (key != null)
        {
            try
            {
                var db = _redis.GetDatabase();
                await db.StringSetAsync(
                    $"{CachePrefix}{keyHash}", 
                    JsonSerializer.Serialize(key), 
                    _cacheTtl);
            }
            catch
            {
                // Ignore caching error
            }
        }

        return key;
    }

    /// <inheritdoc/>
    /// <summary>
    /// Creates a new API key in the database
    /// </summary>
    /// <param name="key">API key to create</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created API key</returns>
    public async Task<ApiKey> CreateAsync(ApiKey key, CancellationToken ct = default)
    {
        _db.ApiKeys.Add(key);
        await _db.SaveChangesAsync(ct);
        return key;
    }

    /// <inheritdoc/>
    /// <summary>
    /// Revokes an API key by marking it as inactive
    /// </summary>
    /// <param name="keyId">Unique identifier of the API key</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if key was revoked and cache invalidated, false otherwise</returns>
    public async Task<bool> RevokeAsync(Guid keyId, CancellationToken ct = default)
    {
        var key = await _db.ApiKeys.FindAsync(new object[] { keyId }, ct);
        if (key == null) return false;
        
        key.IsActive = false;
        await _db.SaveChangesAsync(ct);
        
        // Invalidate cache
        bool cacheInvalidated = false;
        try
        {
            var db = _redis.GetDatabase();
            var deleted = await db.KeyDeleteAsync($"{CachePrefix}{key.KeyHash}");
            cacheInvalidated = deleted;
        }
        catch (Exception ex)
        {
            // Log error - return false as cache was not invalidated
            Console.WriteLine($"Warning: Failed to invalidate Redis cache: {ex.Message}");
            return false;
        }
        
        // Return false if cache was not deleted
        return cacheInvalidated;
    }

    /// <inheritdoc/>
    /// <summary>
    /// Lists all active API keys
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Enumerable collection of active API keys</returns>
    public async Task<IEnumerable<ApiKey>> ListActiveAsync(CancellationToken ct = default)
    {
        return await _db.ApiKeys
            .AsNoTracking()
            .Where(k => k.IsActive)
            .ToListAsync(ct);
    }
}