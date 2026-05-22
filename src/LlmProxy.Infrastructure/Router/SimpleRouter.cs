using LlmProxy.Core.Config;
using LlmProxy.Core.Providers;
using LlmProxy.Core.Router;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LlmProxy.Infrastructure.Router;

public class SimpleRouter : ILlmRouter
{
    private readonly IOptions<LlmConfig> _config;
    private readonly ILogger<SimpleRouter> _logger;
    private readonly Dictionary<string, int> _roundRobinCounters = new();
    private readonly object _lock = new();

    public SimpleRouter(IOptions<LlmConfig> config, ILogger<SimpleRouter> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<ILlmProvider> SelectProviderAsync(
        string requestedModel, 
        IEnumerable<ILlmProvider> availableProviders,
        CancellationToken ct = default)
    {
        // 1. Попытка выбрать по префиксу
        var prefix = ExtractPrefix(requestedModel);
        if (!string.IsNullOrEmpty(prefix))
        {
            var provider = availableProviders.FirstOrDefault(p => 
                p.Prefix.Equals(prefix, StringComparison.OrdinalIgnoreCase));
            if (provider != null)
            {
                _logger.LogDebug("Selected provider {Provider} by prefix {Prefix}", 
                    provider.ProviderName, prefix);
                return provider;
            }
        }

        // 2. Fallback: Round-Robin среди всех доступных провайдеров
        var providers = availableProviders.ToList();
        if (!providers.Any())
            throw new InvalidOperationException("No available LLM providers configured");

        lock (_lock)
        {
            var key = requestedModel.ToLowerInvariant();
            if (!_roundRobinCounters.ContainsKey(key))
                _roundRobinCounters[key] = 0;
            
            var index = _roundRobinCounters[key] % providers.Count;
            _roundRobinCounters[key] = (_roundRobinCounters[key] + 1) % providers.Count;
            
            var selected = providers[index];
            _logger.LogDebug("Selected provider {Provider} via round-robin for model {Model}", 
                selected.ProviderName, requestedModel);
            return selected;
        }
    }

    private string? ExtractPrefix(string modelName)
    {
        if (string.IsNullOrWhiteSpace(modelName)) return null;
        
        var parts = modelName.Split('/', 2);
        return parts.Length > 1 ? parts[0] + "/" : null;
    }

    // Метод для обработки fallback при ошибке провайдера
    public async Task<T> ExecuteWithFallback<T>(
        Func<ILlmProvider, CancellationToken, Task<T>> operation,
        string requestedModel,
        IEnumerable<ILlmProvider> availableProviders,
        int maxRetries = 2,
        CancellationToken ct = default)
    {
        var attempted = new HashSet<string>();
        var lastException = default(Exception);

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            var provider = await SelectProviderAsync(requestedModel, 
                availableProviders.Where(p => !attempted.Contains(p.ProviderName)), ct);
            
            if (provider == null) break;
            
            try
            {
                attempted.Add(provider.ProviderName);
                return await operation(provider, ct);
            }
            catch (HttpRequestException ex) when (ex.StatusCode is null or >= System.Net.HttpStatusCode.InternalServerError)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Provider {Provider} failed (attempt {Attempt}), trying fallback", 
                    provider.ProviderName, attempt + 1);
                // Продолжаем цикл для следующего провайдера
            }
            catch (TaskCanceledException) when (ct.IsCancellationRequested)
            {
                throw; // Не обрабатываем отмену как ошибку провайдера
            }
            catch (Exception ex)
            {
                // Клиентские ошибки (4xx) не триггерят fallback
                _logger.LogError(ex, "Provider {Provider} returned client error", provider.ProviderName);
                throw;
            }
        }

        throw new AggregateException(
            $"All fallback attempts failed for model '{requestedModel}'", 
            lastException ?? new InvalidOperationException("No providers available"));
    }
}