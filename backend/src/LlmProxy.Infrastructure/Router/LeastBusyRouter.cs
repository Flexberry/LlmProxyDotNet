using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LlmProxy.Core.Config;
using LlmProxy.Core.Providers;
using LlmProxy.Core.Router;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LlmProxy.Infrastructure.Router;

/// <summary>
/// Router that selects the least busy provider based on recent request load.
/// Implements weighted round-robin with fallback support.
/// </summary>
public class LeastBusyRouter : ILlmRouter
{
    private readonly IOptions<LlmConfig> _config;
    private readonly ILogger<LeastBusyRouter> _logger;
    
    // Track active requests per provider (thread-safe)
    private readonly ConcurrentDictionary<string, int> _providerLoad = new();
    
    // Round-robin counters for tie-breaking
    private readonly ConcurrentDictionary<string, int> _roundRobinCounters = new();
    
    private readonly object _lock = new();

    public LeastBusyRouter(IOptions<LlmConfig> config, ILogger<LeastBusyRouter> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<ILlmProvider> SelectProviderAsync(
        string requestedModel, 
        IEnumerable<ILlmProvider> availableProviders,
        CancellationToken ct = default)
    {
        if (availableProviders == null || !availableProviders.Any())
            throw new InvalidOperationException("No providers available");

        var providersList = availableProviders.ToList();

        // 1. Try prefix-based selection first
        var prefix = ExtractPrefix(requestedModel);
        if (!string.IsNullOrEmpty(prefix))
        {
            var provider = providersList.FirstOrDefault(p => 
                p.Prefix?.Equals(prefix, StringComparison.OrdinalIgnoreCase) == true);
            if (provider != null)
            {
                _logger.LogDebug("Selected provider {Provider} by prefix {Prefix}", 
                    provider.ProviderName, prefix);
                return provider;
            }
        }

        // 2. Select least busy provider
        ILlmProvider? selected = null;
        int minLoad = int.MaxValue;

        foreach (var provider in providersList)
        {
            var load = _providerLoad.GetOrAdd(provider.ProviderName, _ => 0);
            
            // Skip overloaded providers (optional threshold)
            if (load > 100) // Too many concurrent requests
            {
                _logger.LogDebug("Provider {Provider} is overloaded (load: {Load})", 
                    provider.ProviderName, load);
                continue;
            }

            if (load < minLoad)
            {
                minLoad = load;
                selected = provider;
            }
            else if (load == minLoad && selected != null)
            {
                // Tie-breaking with round-robin
                var key = $"{requestedModel}-{load}";
                var currentCounter = _roundRobinCounters.GetOrAdd(key, _ => 0);
                var selectedCounter = _roundRobinCounters.GetOrAdd(
                    $"{selected.ProviderName}-{load}", _ => 0);
                
                if (currentCounter > selectedCounter)
                {
                    selected = provider;
                }
            }
        }

        if (selected == null)
        {
            // Fallback to first available if all overloaded
            selected = providersList.First();
            _logger.LogWarning("All providers overloaded, falling back to {Provider}", 
                selected.ProviderName);
        }

        // Increment load counter
        IncrementLoad(selected.ProviderName);

        _logger.LogDebug("Selected provider {Provider} via least-busy (load: {Load})", 
            selected.ProviderName, _providerLoad[selected.ProviderName]);

        return selected;
    }

    public async Task<T> ExecuteWithFallback<T>(
        Func<ILlmProvider, CancellationToken, Task<T>> operation,
        string requestedModel,
        IEnumerable<ILlmProvider> availableProviders,
        int maxRetries = 2,
        CancellationToken ct = default)
    {
        if (availableProviders == null)
            throw new ArgumentNullException(nameof(availableProviders));

        var attempted = new HashSet<string>();
        Exception? lastException = null;
        var fallbackSettings = _config.Value?.Fallback ?? new FallbackSettings();

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            var available = availableProviders.Where(p => 
                !attempted.Contains(p.ProviderName) && 
                !fallbackSettings.IgnoreProviders.Contains(p.ProviderName));
            
            if (!available.Any())
                break;

            var provider = await SelectProviderAsync(requestedModel, available, ct);
            if (provider == null) break;

            try
            {
                attempted.Add(provider.ProviderName);
                var result = await operation(provider, ct);
                
                // Decrement load on success
                DecrementLoad(provider.ProviderName);
                return result;
            }
            catch (HttpRequestException ex) when (
                ex.StatusCode == null || 
                ex.StatusCode >= System.Net.HttpStatusCode.InternalServerError)
            {
                lastException = ex;
                DecrementLoad(provider.ProviderName);
                
                _logger.LogWarning(ex, 
                    "Provider {Provider} failed (attempt {Attempt}), trying fallback", 
                    provider.ProviderName, attempt + 1);
                
                // Wait before retry (exponential backoff)
                if (attempt < maxRetries)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(100 * (attempt + 1)), ct);
                }
            }
            catch (TaskCanceledException) when (ct.IsCancellationRequested)
            {
                DecrementLoad(provider.ProviderName);
                throw;
            }
            catch (Exception ex)
            {
                DecrementLoad(provider.ProviderName);
                _logger.LogError(ex, "Provider {Provider} returned client error", provider.ProviderName);
                throw;
            }
        }

        throw new AggregateException(
            $"All fallback attempts failed for model '{requestedModel}'", 
            lastException ?? new InvalidOperationException("No providers available"));
    }

    private void IncrementLoad(string providerName)
    {
        _providerLoad.AddOrUpdate(providerName, 1, (_, current) => current + 1);
    }

    private void DecrementLoad(string providerName)
    {
        _providerLoad.AddOrUpdate(providerName, 0, (_, current) => 
            current > 0 ? current - 1 : 0);
    }

    private string? ExtractPrefix(string? modelName)
    {
        if (string.IsNullOrWhiteSpace(modelName)) return null;
        
        var parts = modelName.Split('/', 2);
        return parts.Length > 1 ? parts[0] + "/" : null;
    }
}
