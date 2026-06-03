using LlmProxy.Core.Providers;

namespace LlmProxy.Core.Router;

/// <summary>
/// Интерфейс маршрутизатора LLM провайдеров
/// </summary>
public interface ILlmRouter
{
    /// <summary>
    /// Выбирает подходящего провайдера для запрошенной модели
    /// </summary>
    /// <param name="requestedModel">Запрошенное имя модели</param>
    /// <param name="availableProviders">Список доступных провайдеров</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Выбранный провайдер</returns>
    Task<ILlmProvider> SelectProviderAsync(
        string requestedModel, 
        IEnumerable<ILlmProvider> availableProviders,
        CancellationToken ct = default);

    /// <summary>
    /// Выполняет операцию с поддержкой fallback на альтернативные провайдеры при ошибках
    /// </summary>
    /// <typeparam name="T">Тип результата операции</typeparam>
    /// <param name="operation">Асинхронная операция для выполнения</param>
    /// <param name="requestedModel">Запрошенное имя модели</param>
    /// <param name="availableProviders">Список доступных провайдеров</param>
    /// <param name="maxRetries">Максимальное количество попыток переключения</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Результат операции</returns>
    Task<T> ExecuteWithFallback<T>(
        Func<ILlmProvider, CancellationToken, Task<T>> operation,
        string requestedModel,
        IEnumerable<ILlmProvider> availableProviders,
        int maxRetries = 2,
        CancellationToken ct = default);
}