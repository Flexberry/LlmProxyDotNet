using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace LlmProxy.Core.Utils;

/// <summary>
/// Вспомогательный класс для работы с API ключами: генерация, хеширование, проверка разрешений
/// </summary>
public static class KeyHelper
{
    /// <summary>
    /// Длина генерируемого ключа в байтах (32 байта = 256 бит)
    /// </summary>
    private const int KeyLength = 32;
    
    /// <summary>
    /// Генерирует новый API ключ со случайной последовательностью
    /// </summary>
    /// <param name="prefix">Префикс ключа (по умолчанию "sk")</param>
    /// <returns>Сгенерированный API ключ в формате "prefix_XXXXXXXX"</returns>
    public static string GenerateApiKey(string prefix = "sk")
    {
        var bytes = new byte[KeyLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return $"{prefix}_{Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "")}";
    }
    
    /// <summary>
    /// Хеширует API ключ с помощью алгоритма SHA256
    /// </summary>
    /// <param name="key">Плоский API ключ для хеширования</param>
    /// <returns>SHA256 хеш ключа в нижнем регистре</returns>
    public static string HashKey(string key)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
    
    /// <summary>
    /// Проверяет, имеет ли ключ разрешение на доступ к указанной модели
    /// </summary>
    /// <param name="permissions">Строка разрешений (JSON массив моделей или "*" для всех)</param>
    /// <param name="model">Название модели для проверки доступа</param>
    /// <returns>True, если доступ разрешён; false в противном случае</returns>
    public static bool HasModelPermission(string? permissions, string model)
    {
        // Пустые разрешения = доступ запрещён
        if (string.IsNullOrWhiteSpace(permissions))
            return false;
        
        // Уолдкард разрешает всё
        if (permissions.Trim() == "*")
            return true;
        
        // Точное совпадение в списке разрешённых моделей
        var allowedModels = permissions
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(m => m.Trim());
        
        return allowedModels.Contains(model, StringComparer.OrdinalIgnoreCase);
    }
}