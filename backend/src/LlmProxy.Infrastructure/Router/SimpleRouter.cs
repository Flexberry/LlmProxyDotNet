using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using LlmProxy.Core.Config;
using LlmProxy.Core.Providers;
using LlmProxy.Core.Router;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LlmProxy.Infrastructure.Router;

/// <summary>
/// Простой маршрутизатор с поддержкой round-robin и fallback
/// </summary>
public class SimpleRouter : ILlmRouter
{
    private readonly IOptions<LlmConfig> _config;
    private readonly ILogger<SimpleRouter> _logger;
    private readonly Dictionary<string, int> _roundRobinCounters = new();
    private readonly object _lock = new();

    /// <summary>
    /// Создаёт экземпляр маршрутизатора
    /// </summary>
    /// <param name="config">Конфигурация LLM</param>
    /// <param name="logger">Журналлер</param>
    public SimpleRouter(IOptions<LlmConfig> config, ILogger<SimpleRouter> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<ILlmProvider> SelectProviderAsync(
        string requestedModel, 
        IEnumerable<ILlmProvider> availableProviders,
        CancellationToken ct = default)
    {
        if (availableProviders == null || !availableProviders.Any())
            throw new InvalidOperationException("No providers available");

        var providersList = availableProviders.ToList();

        // 1. Попытка выбрать по префиксу
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

        // 2. Fallback: Round-Robin среди всех доступных провайдеров
        lock (_lock)
        {
            var key = requestedModel?.ToLowerInvariant() ?? "default";
            if (!_roundRobinCounters.ContainsKey(key))
                _roundRobinCounters[key] = 0;
            
            var index = _roundRobinCounters[key] % providersList.Count;
            _roundRobinCounters[key] = (_roundRobinCounters[key] + 1) % providersList.Count;
            
            var selected = providersList[index];
            _logger.LogDebug("Selected provider {Provider} via round-robin for model {Model}", 
                selected.ProviderName, requestedModel);
            return selected;
        }
    }

    /// <inheritdoc/>
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

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            var available = availableProviders.Where(p => !attempted.Contains(p.ProviderName));
            if (!available.Any())
                break;

            var provider = await SelectProviderAsync(requestedModel, available, ct);
            if (provider == null) break;
            
            try
            {
                attempted.Add(provider.ProviderName);
                return await operation(provider, ct);
            }
            catch (HttpRequestException ex) 
                when (ex.StatusCode == null || ex.StatusCode >= HttpStatusCode.InternalServerError)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Provider {Provider} failed (attempt {Attempt}), trying fallback", 
                    provider.ProviderName, attempt + 1);
            }
            catch (TaskCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Provider {Provider} returned client error", provider.ProviderName);
                throw;
            }
        }

        throw new AggregateException(
            $"All fallback attempts failed for model '{requestedModel}'", 
            lastException ?? new InvalidOperationException("No providers available"));
    }

    /// <summary>
    /// Извлекает префикс из имени модели (например, "ollama/" из "ollama/llama3")
    /// </summary>
    /// <param name="modelName">Имя модели</param>
    /// <returns>Префикс модели или null</returns>
    private string? ExtractPrefix(string? modelName)
    {
        if (string.IsNullOrWhiteSpace(modelName)) return null;
        
        var parts = modelName.Split('/', 2);
        return parts.Length > 1 ? parts[0] + "/" : null;
    }
}