using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LlmProxy.Core.Utils;

public static class KeyHelper
{
    // Генерация ключа формата sk-... (32 байта, base64)
    public static string GenerateApiKey(string prefix = "sk")
    {
        var bytes = new byte[24];
        RandomNumberGenerator.Fill(bytes);
        var key = Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
        return $"{prefix}_{key}";
    }

    // SHA256 хэширование для безопасного хранения
    public static string HashKey(string key)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(key);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    // Парсинг разрешений моделей (JSON массив или "*")
    public static bool HasModelPermission(string permissions, string modelName)
    {
        if (string.IsNullOrWhiteSpace(permissions) || permissions == "*")
            return true;
        
        try
        {
            var allowedModels = JsonSerializer.Deserialize<string[]>(permissions);
            return allowedModels?.Any(m => m == "*" || m == modelName) == true;
        }
        catch
        {
            return false;
        }
    }
}