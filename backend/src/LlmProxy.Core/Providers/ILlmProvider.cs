using LlmProxy.Core.Models.Dto;

namespace LlmProxy.Core.Providers;

/// <summary>
/// Интерфейс для провайдера LLM моделей
/// </summary>
public interface ILlmProvider
{
    /// <summary>
    /// Имя провайдера (например, "OpenAI", "Ollama")
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Префикс имени модели для маршрутизации (например, "openai", "ollama")
    /// </summary>
    string Prefix { get; }
    
    /// <summary>
    /// Создаёт завершение чата (chat completion)
    /// </summary>
    /// <param name="request">Запрос на создание завершения чата</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Ответ с завершением чата</returns>
    Task<ChatCompletionResponse> CreateChatCompletionAsync(
        ChatCompletionRequest request, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Создаёт потоковое завершение чата (streaming chat completion)
    /// </summary>
    /// <param name="request">Запрос на создание завершения чата</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Асинхронный поток фрагментов ответа</returns>
    IAsyncEnumerable<ChatCompletionChunk> CreateChatCompletionStreamAsync(
        ChatCompletionRequest request, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Создаёт эмбеддинги для входных текстов
    /// </summary>
    /// <param name="request">Запрос на создание эмбеддингов</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Ответ с эмбеддингами</returns>
    Task<EmbeddingResponse> CreateEmbeddingsAsync(
        EmbeddingRequest request, 
        CancellationToken ct = default);
}