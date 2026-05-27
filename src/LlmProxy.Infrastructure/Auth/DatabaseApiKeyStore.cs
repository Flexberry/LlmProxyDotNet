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
        // Пытаемся получить из кэша Redis (если доступен)
        try
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
        }
        catch (Exception ex)
        {
            // Redis недоступен - продолжаем без кэша
            Console.WriteLine($"Warning: Redis cache unavailable: {ex.Message}");
        }
        
        // Запрос к БД (fallback если Redis недоступен или кэш не найден)
        var key = await _db.ApiKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash && k.IsActive, ct);

        // Пытаемся закэшировать результат (без ошибки если Redis недоступен)
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
                // Игнорируем ошибку кэширования
            }
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
        
        // Инвалидация кэша (без ошибки если Redis недоступен)
        try
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync($"{CachePrefix}{key.KeyHash}");
        }
        catch (Exception ex)
        {
            // Логгируем, но не прерываем операцию, если Redis недоступен
            // Кэш устареет автоматически после TTL (5 минут)
            Console.WriteLine($"Warning: Failed to invalidate Redis cache: {ex.Message}");
        }
        
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