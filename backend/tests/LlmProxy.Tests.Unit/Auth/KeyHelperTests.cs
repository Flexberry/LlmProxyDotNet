using LlmProxy.Core.Utils;
using Xunit;

namespace LlmProxy.Tests.Unit.Auth;

public class KeyHelperTests
{
    [Fact]
    public void GenerateApiKey_ReturnsValidFormat()
    {
        var key = KeyHelper.GenerateApiKey("sk");
        
        Assert.StartsWith("sk_", key);
        Assert.True(key.Length >= 40, $"Key length {key.Length} is too short");
    }

    [Fact]
    public void HashKey_ProducesConsistentSha256()
    {
        var key = "sk_test_abc123";
        
        var hash1 = KeyHelper.HashKey(key);
        var hash2 = KeyHelper.HashKey(key);
        
        Assert.Equal(hash1, hash2);
        Assert.Equal(64, hash1.Length);
    }

    [Theory]
    [InlineData("*", "ollama/llama3", true)]
    [InlineData("ollama/llama3", "ollama/llama3", true)]
    [InlineData("openai/gpt-4", "ollama/llama3", false)]
    [InlineData("", "any", false)]
    public void HasModelPermission_ValidatesCorrectly(string permissions, string model, bool expected)
    {
        var result = KeyHelper.HasModelPermission(permissions, model);
        Assert.Equal(expected, result);
    }
}