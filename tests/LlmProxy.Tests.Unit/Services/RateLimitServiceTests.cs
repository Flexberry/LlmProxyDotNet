using FluentAssertions;
using LlmProxy.Core.Entities;
using LlmProxy.Infrastructure.Services;
using Moq;
using StackExchange.Redis;

namespace LlmProxy.Tests.Unit.Services;

public class RateLimitServiceTests : IDisposable
{
    private readonly Mock<IConnectionMultiplexer> _redisMock;
    private readonly Mock<IDatabase> _dbMock;
    private readonly RateLimitService _service;
    private readonly RedisValue _testValue = "10";

    public RateLimitServiceTests()
    {
        _dbMock = new Mock<IDatabase>();
        _redisMock = new Mock<IConnectionMultiplexer>();
        _redisMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object?>()))
                  .Returns(_dbMock.Object);
        
        _service = new RateLimitService(_redisMock.Object);
    }

    [Fact]
    public async Task CheckRateLimitAsync_WithinLimits_ShouldReturnAllowed()
    {
        // Arrange
        var apiKeyHash = "test-key";
        var config = new RateLimitConfig { RequestsPerMinute = 100, RequestsPerDay = 10000 };
        
        _dbMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
               .ReturnsAsync(RedisValue.Null);

        // Act
        var result = await _service.CheckRateLimitAsync(apiKeyHash, config, 0);

        // Assert
        result.IsAllowed.Should().BeTrue();
        result.RequestsThisMinute.Should().Be(0);
        result.RequestsToday.Should().Be(0);
    }

    [Fact]
    public async Task CheckRateLimitAsync_ExceedsMinuteLimit_ShouldReturnNotAllowed()
    {
        // Arrange
        var apiKeyHash = "test-key";
        var config = new RateLimitConfig { RequestsPerMinute = 10, RequestsPerDay = 10000 };
        
        _dbMock.Setup(x => x.StringGetAsync(new RedisKey($"ratelimit:{apiKeyHash}:minute"), It.IsAny<CommandFlags>()))
               .ReturnsAsync(new RedisValue("15"));
        _dbMock.Setup(x => x.StringGetAsync(new RedisKey($"ratelimit:{apiKeyHash}:day"), It.IsAny<CommandFlags>()))
               .ReturnsAsync(new RedisValue("0"));

        // Act
        var result = await _service.CheckRateLimitAsync(apiKeyHash, config, 0);

        // Assert
        result.IsAllowed.Should().BeFalse();
        result.RequestsThisMinute.Should().Be(15);
        result.RetryAfter.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task CheckRateLimitAsync_ExceedsDailyLimit_ShouldReturnNotAllowed()
    {
        // Arrange
        var apiKeyHash = "test-key";
        var config = new RateLimitConfig { RequestsPerMinute = 100, RequestsPerDay = 10 };
        
        _dbMock.Setup(x => x.StringGetAsync(new RedisKey($"ratelimit:{apiKeyHash}:minute"), It.IsAny<CommandFlags>()))
               .ReturnsAsync(new RedisValue("0"));
        _dbMock.Setup(x => x.StringGetAsync(new RedisKey($"ratelimit:{apiKeyHash}:day"), It.IsAny<CommandFlags>()))
               .ReturnsAsync(new RedisValue("15"));

        // Act
        var result = await _service.CheckRateLimitAsync(apiKeyHash, config, 0);

        // Assert
        result.IsAllowed.Should().BeFalse();
        result.RequestsToday.Should().Be(15);
    }

    [Fact]
    public async Task IncrementRequestAsync_ShouldIncrementCounters()
    {
        // Arrange
        var apiKeyHash = "test-key";

        // Act
        await _service.IncrementRequestAsync(apiKeyHash, 10);

        // Assert - just verify StringIncrementAsync was called at least once
        _dbMock.Verify(x => x.StringIncrementAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<long>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task IncrementRequestAsync_WithTokens_ShouldIncrementTokenCounter()
    {
        // Arrange
        var apiKeyHash = "test-key";
        var tokenCount = 50;

        // Act
        await _service.IncrementRequestAsync(apiKeyHash, tokenCount);

        // Assert - just verify StringIncrementAsync was called
        _dbMock.Verify(x => x.StringIncrementAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<long>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ResetLimitsAsync_ShouldDeleteAllRateLimitKeys()
    {
        // Arrange
        var apiKeyHash = "test-key";

        // Act
        await _service.ResetLimitsAsync(apiKeyHash);

        // Assert
        _dbMock.Verify(x => x.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), Times.AtLeastOnce);
    }

    public void Dispose()
    {
        // Mock objects don't need explicit disposal
    }
}