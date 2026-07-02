using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace LlmProxy.Core.Utils;

public static class KeyHelper
{
    private const int KeyLength = 32;
    
    public static string GenerateApiKey(string prefix = "sk")
    {
        var bytes = new byte[KeyLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return $"{prefix}_{Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "")}";
    }
    
    public static string HashKey(string key)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
    
    public static bool HasModelPermission(string? permissions, string model)
    {
        // ИСПРАВЛЕНИЕ: Пустые права = запрет доступа
        if (string.IsNullOrWhiteSpace(permissions))
            return false;
        
        // Wildcard разрешает всё
        if (permissions.Trim() == "*")
            return true;
        
        // Точное совпадение по списку разрешённых моделей
        var allowedModels = permissions
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(m => m.Trim());
        
        return allowedModels.Contains(model, StringComparer.OrdinalIgnoreCase);
    }
}