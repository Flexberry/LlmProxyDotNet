using LlmProxy.Core.Providers;

namespace LlmProxy.Infrastructure.Providers;

public class ProviderFactory
{
    private readonly IEnumerable<ILlmProvider> _providers;
    private readonly Dictionary<string, ILlmProvider> _prefixMap;

    public ProviderFactory(IEnumerable<ILlmProvider> providers)
    {
        _providers = providers;
        _prefixMap = providers.ToDictionary(p => p.Prefix.ToLowerInvariant(), p => p);
    }

    public ILlmProvider? GetByPrefix(string modelName)
    {
        if (string.IsNullOrWhiteSpace(modelName)) return null;
        
        foreach (var (prefix, provider) in _prefixMap)
        {
            if (modelName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return provider;
        }
        return null;
    }

    public IEnumerable<ILlmProvider> GetAll() => _providers;
}