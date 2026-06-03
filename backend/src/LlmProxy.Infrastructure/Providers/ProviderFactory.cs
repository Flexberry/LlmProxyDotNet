using LlmProxy.Core.Providers;

namespace LlmProxy.Infrastructure.Providers;

/// <summary>
/// Фабрика для получения провайдеров LLM по префиксу имени модели
/// </summary>
public class ProviderFactory
{
    private readonly IEnumerable<ILlmProvider> _providers;
    private readonly Dictionary<string, ILlmProvider> _prefixMap;

    /// <summary>
    /// Создаёт экземпляр фабрики
    /// </summary>
    /// <param name="providers">Список доступных провайдеров</param>
    public ProviderFactory(IEnumerable<ILlmProvider> providers)
    {
        _providers = providers;
        _prefixMap = providers.ToDictionary(p => p.Prefix.ToLowerInvariant(), p => p);
    }

    /// <summary>
    /// Получает провайдера по префиксу имени модели
    /// </summary>
    /// <param name="modelName">Имя модели (например, "ollama/llama3")</param>
    /// <returns>Провайдер, соответствующий префиксу, или null</returns>
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

    /// <summary>
    /// Получает список всех зарегистрированных провайдеров
    /// </summary>
    /// <returns>Перечень всех провайдеров</returns>
    public IEnumerable<ILlmProvider> GetAll() => _providers;
}