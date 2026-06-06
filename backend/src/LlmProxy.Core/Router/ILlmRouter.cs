using LlmProxy.Core.Providers;

namespace LlmProxy.Core.Router;

public interface ILlmRouter
{
    Task<ILlmProvider> SelectProviderAsync(
        string requestedModel, 
        IEnumerable<ILlmProvider> availableProviders,
        CancellationToken ct = default);

    Task<T> ExecuteWithFallback<T>(
        Func<ILlmProvider, CancellationToken, Task<T>> operation,
        string requestedModel,
        IEnumerable<ILlmProvider> availableProviders,
        int maxRetries = 2,
        CancellationToken ct = default);
}