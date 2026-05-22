using StackExchange.Redis;
using LlmProxy.Core.Auth;
using LlmProxy.Core.Entities;
using LlmProxy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LlmProxy.Infrastructure.Auth;

public class DatabaseApiKeyStore : IApiKeyStore
{
    private readonly LlmProxyDbContext _db;
    private readonly IConnectionMultiplexer _redis;
    private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(5);
    private const string CachePrefix = "api_key:";

    public DatabaseApiKeyStore(LlmProxyDbContext db, IConnectionMultiplexer redis)
    {
        _db = db;
        _redis = redis;
    }

    public async Task<ApiKey?> GetByKeyHashAsync(string keyHash, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var cached = await db.StringGetAsync($"{CachePrefix}{keyHash}");
        
        if (cached.HasValue)
        {
            // Явное приведение к string устраняет неоднозначность
            var cachedString = cached.ToString();
            
            // Если в кэше маркер отсутствия ключа
            if (cachedString == "null") return null;
            
            return JsonSerializer.Deserialize<ApiKey>(cachedString);
        }

        // 2. Запрос к БД
        var key = await _db.ApiKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash && k.IsActive, ct);

        // 3. Кэширование результата
        if (key != null)
        {
            await db.StringSetAsync(
                $"{CachePrefix}{keyHash}", 
                JsonSerializer.Serialize(key), 
                _cacheTtl);
        }
        else
        {
            // Кэшируем отсутствие ключа на 1 минуту
            await db.StringSetAsync($"{CachePrefix}{keyHash}", "null", TimeSpan.FromMinutes(1));
        }

        return key;
    }

    public async Task<ApiKey> CreateAsync(ApiKey key, CancellationToken ct = default)
    {
        _db.ApiKeys.Add(key);
        await _db.SaveChangesAsync(ct);
        return key;
    }

    public async Task<bool> RevokeAsync(Guid keyId, CancellationToken ct = default)
    {
        var key = await _db.ApiKeys.FindAsync(new object[] { keyId }, ct);
        if (key == null) return false;
        
        key.IsActive = false;
        await _db.SaveChangesAsync(ct);
        
        // Инвалидация кэша
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync($"{CachePrefix}{key.KeyHash}");
        
        return true;
    }

    public async Task<IEnumerable<ApiKey>> ListActiveAsync(CancellationToken ct = default)
    {
        return await _db.ApiKeys
            .AsNoTracking()
            .Where(k => k.IsActive)
            .ToListAsync(ct);
    }
}