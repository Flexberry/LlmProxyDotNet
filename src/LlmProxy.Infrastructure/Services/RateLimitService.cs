using System.Collections.Concurrent;
using StackExchange.Redis;
using LlmProxy.Core.Entities;

namespace LlmProxy.Infrastructure.Services;

/// <summary>
/// Сервис для rate limiting API запросов
/// </summary>
public class RateLimitService : IRateLimitService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly TimeSpan _minuteWindow = TimeSpan.FromMinutes(1);
    private readonly TimeSpan _dayWindow = TimeSpan.FromDays(1);
    
    private readonly ConcurrentDictionary<string, RequestCounter> _inMemoryCounters = new();

    public RateLimitService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<RateLimitCheckResult> CheckRateLimitAsync(
        string apiKeyHash, 
        RateLimitConfig? config, 
        int tokenCount = 0,
        CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var now = DateTime.UtcNow;
        
        var minuteKey = $"ratelimit:{apiKeyHash}:minute";
        var dayKey = $"ratelimit:{apiKeyHash}:day";
        
        var requestsThisMinute = await GetCounterAsync(db, minuteKey, _minuteWindow, ct);
        var requestsToday = await GetCounterAsync(db, dayKey, _dayWindow, ct);
        
        var maxRequestsPerMinute = config?.RequestsPerMinute ?? 100;
        var maxRequestsPerDay = config?.RequestsPerDay ?? 10000;
        var maxTokensPerMinute = config?.TokensPerMinute ?? 100000;
        
        var isRateLimited = false;
        var retryAfter = TimeSpan.Zero;
        
        if (requestsThisMinute >= maxRequestsPerMinute)
        {
            isRateLimited = true;
            retryAfter = _minuteWindow;
        }
        else if (requestsToday >= maxRequestsPerDay)
        {
            isRateLimited = true;
            retryAfter = _dayWindow;
        }
        else if (tokenCount > 0 && requestsThisMinute + tokenCount > maxTokensPerMinute)
        {
            isRateLimited = true;
            retryAfter = _minuteWindow;
        }
        
        return new RateLimitCheckResult
        {
            IsAllowed = !isRateLimited,
            RequestsThisMinute = requestsThisMinute,
            RequestsToday = requestsToday,
            RetryAfter = retryAfter,
            ResetAt = now + _minuteWindow
        };
    }

    public async Task IncrementRequestAsync(string apiKeyHash, int tokenCount = 0, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        
        var minuteKey = $"ratelimit:{apiKeyHash}:minute";
        var dayKey = $"ratelimit:{apiKeyHash}:day";
        
        await db.StringIncrementAsync(minuteKey, 1);
        await db.StringIncrementAsync(dayKey, 1);
        
        if (tokenCount > 0)
        {
            var tokensKey = $"ratelimit:{apiKeyHash}:tokens:minute";
            await db.StringIncrementAsync(tokensKey, tokenCount);
        }
    }

    public async Task ResetLimitsAsync(string apiKeyHash, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        
        var minuteKey = $"ratelimit:{apiKeyHash}:minute";
        var dayKey = $"ratelimit:{apiKeyHash}:day";
        var tokensKey = $"ratelimit:{apiKeyHash}:tokens:minute";
        
        await db.KeyDeleteAsync(minuteKey);
        await db.KeyDeleteAsync(dayKey);
        await db.KeyDeleteAsync(tokensKey);
    }

    private async Task<int> GetCounterAsync(IDatabase db, string key, TimeSpan window, CancellationToken ct)
    {
        var value = await db.StringGetAsync(key);
        return value.IsNullOrEmpty ? 0 : (int)value;
    }
}

public class RateLimitCheckResult
{
    public bool IsAllowed { get; set; }
    public int RequestsThisMinute { get; set; }
    public int RequestsToday { get; set; }
    public TimeSpan RetryAfter { get; set; }
    public DateTime ResetAt { get; set; }
}

public interface IRateLimitService
{
    Task<RateLimitCheckResult> CheckRateLimitAsync(string apiKeyHash, RateLimitConfig? config, int tokenCount = 0, CancellationToken ct = default);
    Task IncrementRequestAsync(string apiKeyHash, int tokenCount = 0, CancellationToken ct = default);
    Task ResetLimitsAsync(string apiKeyHash, CancellationToken ct = default);
}

internal class RequestCounter
{
    public int Requests { get; set; }
    public DateTime WindowStart { get; set; } = DateTime.UtcNow;
}