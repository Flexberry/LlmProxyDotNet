using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace LlmProxy.Core.Utils;

/// <summary>
/// Helper class for API key operations: generation, hashing, permission checking
/// </summary>
public static class KeyHelper
{
    /// <summary>
    /// Length of generated key in bytes (32 bytes = 256 bits)
    /// </summary>
    private const int KeyLength = 32;
    
    /// <summary>
    /// Generates a new API key with a secure random sequence
    /// </summary>
    /// <param name="prefix">Key prefix (default "sk")</param>
    /// <returns>Generated API key in format "prefix_XXXXXXXX"</returns>
    public static string GenerateApiKey(string prefix = "sk")
    {
        var bytes = new byte[KeyLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return $"{prefix}_{Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "")}";
    }
    
    /// <summary>
    /// Hashes an API key using SHA256 algorithm
    /// </summary>
    /// <param name="key">Plain API key to hash</param>
    /// <returns>SHA256 hash of the key in lowercase</returns>
    public static string HashKey(string key)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
    
    /// <summary>
    /// Checks if a key has permission to access the specified model
    /// </summary>
    /// <param name="permissions">Permissions string (JSON array of models or "*" for all)</param>
    /// <param name="model">Model name to check access for</param>
    /// <returns>True if access is allowed; false otherwise</returns>
    public static bool HasModelPermission(string? permissions, string model)
    {
        // Empty permissions = access denied
        if (string.IsNullOrWhiteSpace(permissions))
            return false;
        
        // Wildcard allows everything
        if (permissions.Trim() == "*")
            return true;
        
        // Exact match in allowed models list
        var allowedModels = permissions
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(m => m.Trim());
        
        return allowedModels.Contains(model, StringComparer.OrdinalIgnoreCase);
    }
}