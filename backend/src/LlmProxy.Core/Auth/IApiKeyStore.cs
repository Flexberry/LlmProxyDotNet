using LlmProxy.Core.Entities;

namespace LlmProxy.Core.Auth;

/// <summary>
/// Интерфейс для хранилища API ключей
/// </summary>
public interface IApiKeyStore
{
    /// <summary>
    /// Получает API ключ по его хешу
    /// </summary>
    /// <param name="keyHash">SHA256 хеш API ключа</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>API ключ или null, если не найден</returns>
    Task<ApiKey?> GetByKeyHashAsync(string keyHash, CancellationToken ct = default);

    /// <summary>
    /// Создаёт новый API ключ в хранилище
    /// </summary>
    /// <param name="key">Сущность API ключа для создания</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Созданный API ключ</returns>
    Task<ApiKey> CreateAsync(ApiKey key, CancellationToken ct = default);

    /// <summary>
    /// Отзывает API ключ по его ID
    /// </summary>
    /// <param name="keyId">Уникальный идентификатор ключа</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>True, если ключ успешно отозван; false, если ключ не найден</returns>
    Task<bool> RevokeAsync(Guid keyId, CancellationToken ct = default);

    /// <summary>
    /// Получает список активных API ключей
    /// </summary>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Перечень активных API ключей</returns>
    Task<IEnumerable<ApiKey>> ListActiveAsync(CancellationToken ct = default);
}