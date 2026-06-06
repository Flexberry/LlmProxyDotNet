using LlmProxy.Core.Entities;

namespace LlmProxy.Core.Auth;

public interface IApiKeyStore
{
    Task<ApiKey?> GetByKeyHashAsync(string keyHash, CancellationToken ct = default);
    Task<ApiKey> CreateAsync(ApiKey key, CancellationToken ct = default);
    Task<bool> RevokeAsync(Guid keyId, CancellationToken ct = default);
    Task<IEnumerable<ApiKey>> ListActiveAsync(CancellationToken ct = default);
}