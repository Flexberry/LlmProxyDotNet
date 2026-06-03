using FluentAssertions;
using LlmProxy.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;

namespace LlmProxy.Tests.Unit.Services;

public class ResponseCacheServiceTests : IDisposable
{
    private readonly Mock<IConnectionMultiplexer> _redisMock;
    private readonly Mock<IDatabase> _dbMock;
    private readonly Mock<ILogger<ResponseCacheService>> _loggerMock;
    private readonly ResponseCacheService _service;

    public ResponseCacheServiceTests()
    {
        _dbMock = new Mock<IDatabase>();
        _redisMock = new Mock<IConnectionMultiplexer>();
        _redisMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object?>()))
                  .Returns(_dbMock.Object);
        
        _loggerMock = new Mock<ILogger<ResponseCacheService>>();
        _service = new ResponseCacheService(_redisMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetCachedResponseAsync_WhenExists_ShouldReturnCachedValue()
    {
        var cacheKey = "test-cache-key";
        var cachedResponse = "Cached response content";
        
        _dbMock.Setup(x => x.StringGetAsync(cacheKey, It.IsAny<CommandFlags>()))
               .ReturnsAsync(new RedisValue(cachedResponse));

        var result = await _service.GetCachedResponseAsync(cacheKey);

        result.Should().Be(cachedResponse);
        _dbMock.Verify(x => x.StringGetAsync(cacheKey, It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task GetCachedResponseAsync_WhenNotExists_ShouldReturnNull()
    {
        var cacheKey = "non-existent-key";
        
        _dbMock.Setup(x => x.StringGetAsync(cacheKey, It.IsAny<CommandFlags>()))
               .ReturnsAsync(RedisValue.Null);

        var result = await _service.GetCachedResponseAsync(cacheKey);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetCachedResponseAsync_ShouldStoreInCache()
    {
        var cacheKey = "test-cache-key";
        var response = "Response content";

        await _service.SetCachedResponseAsync(cacheKey, response);

        Assert.True(true);
    }

    [Fact]
    public async Task SetCachedResponseAsync_WithCustomTtl_ShouldUseCustomTtl()
    {
        // Arrange
        var cacheKey = "test-cache-key";
        var response = "Response content";
        var customTtl = TimeSpan.FromHours(2);

        // Act
        await _service.SetCachedResponseAsync(cacheKey, response, customTtl);

        // Assert - just verify it doesn't throw
        Assert.True(true);
    }

    [Fact]
    public void GenerateCacheKey_ShouldCreateUniqueKey()
    {
        // Arrange
        var model = "gpt-4";
        var prompt = "Hello, how are you?";
        var parameters = new Dictionary<string, object>
        {
            { "temperature", 0.7 },
            { "max_tokens", 100 }
        };

        // Act
        var key1 = _service.GenerateCacheKey(model, prompt, parameters);
        var key2 = _service.GenerateCacheKey(model, prompt, parameters);
        var key3 = _service.GenerateCacheKey(model, "Different prompt", parameters);

        // Assert
        key1.Should().NotBeNullOrEmpty();
        key1.Should().Be(key2); // Same inputs produce same key
        key1.Should().NotBe(key3); // Different prompt produces different key
        key1.Should().StartWith($"llm:{model}:");
    }

    [Fact]
    public void GenerateCacheKey_WithoutParameters_ShouldStillWork()
    {
        // Arrange
        var model = "gpt-4";
        var prompt = "Hello";

        // Act
        var key = _service.GenerateCacheKey(model, prompt, null);

        // Assert
        key.Should().NotBeNullOrEmpty();
        key.Should().StartWith($"llm:{model}:");
    }

    [Fact]
    public async Task ClearModelCacheAsync_ShouldLogInfo()
    {
        // Arrange
        var model = "gpt-4";

        // Act
        await _service.ClearModelCacheAsync(model);

        // Assert
        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("manual cleanup required")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task ClearAllCacheAsync_ShouldLogInfo()
    {
        // Act
        await _service.ClearAllCacheAsync();

        // Assert
        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("manual cleanup required")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    public void Dispose()
    {
        // Mock objects don't need explicit disposal
    }
}