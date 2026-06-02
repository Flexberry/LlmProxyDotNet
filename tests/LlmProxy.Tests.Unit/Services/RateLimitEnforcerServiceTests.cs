using FluentAssertions;
using LlmProxy.Core.Entities;
using LlmProxy.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace LlmProxy.Tests.Unit.Services;

public class RateLimitEnforcerServiceTests
{
    private readonly Mock<IRateLimitService> _mockRateLimitService;
    private readonly Mock<IBudgetService> _mockBudgetService;
    private readonly Mock<IWebhookService> _mockWebhookService;
    private readonly Mock<ILogger<RateLimitEnforcerService>> _mockLogger;
    private readonly RateLimitEnforcerService _service;

    public RateLimitEnforcerServiceTests()
    {
        _mockRateLimitService = new Mock<IRateLimitService>();
        _mockBudgetService = new Mock<IBudgetService>();
        _mockWebhookService = new Mock<IWebhookService>();
        _mockLogger = new Mock<ILogger<RateLimitEnforcerService>>();
        
        _service = new RateLimitEnforcerService(
            _mockRateLimitService.Object,
            _mockBudgetService.Object,
            _mockWebhookService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task CheckAndEnforceAsync_WithNoLimits_ShouldReturnAllowed()
    {
        // Arrange
        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            KeyHash = "test-hash",
            Permissions = "*",
            IsActive = true
        };

        // Act
        var result = await _service.CheckAndEnforceAsync(apiKey, "test-model");

        // Assert
        result.IsAllowed.Should().BeTrue();
        result.Reason.Should().BeNull();
    }

    [Fact]
    public async Task CheckAndEnforceAsync_WithRateLimitExceeded_ShouldReturnNotAllowed()
    {
        // Arrange
        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            KeyHash = "test-hash",
            Permissions = "*",
            IsActive = true,
            RateLimitConfigJson = "{\"RequestsPerMinute\": 10}"
        };

        _mockRateLimitService.Setup(x => x.CheckRateLimitAsync(
                apiKey.KeyHash, It.IsAny<RateLimitConfig>(), 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RateLimitCheckResult
            {
                IsAllowed = false,
                RequestsThisMinute = 15,
                RequestsToday = 15,
                RetryAfter = TimeSpan.FromMinutes(1),
                ResetAt = DateTime.UtcNow.AddMinutes(1)
            });

        // Act
        var result = await _service.CheckAndEnforceAsync(apiKey, "test-model");

        // Assert
        result.IsAllowed.Should().BeFalse();
        result.Reason.Should().Be("Rate limit exceeded");
        result.RetryAfter.Should().BeGreaterThan(TimeSpan.Zero);
        
        _mockWebhookService.Verify(x => x.SendRateLimitExceededAsync(
            apiKey.KeyHash, "test-model", It.IsAny<RateLimitConfig>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckAndEnforceAsync_WithBudgetExceeded_ShouldReturnNotAllowed()
    {
        // Arrange
        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            KeyHash = "test-hash",
            Permissions = "*",
            IsActive = true
        };

        _mockBudgetService.Setup(x => x.CheckBudgetAsync(
                apiKey.Id.ToString(), "ApiKey", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BudgetCheckResult
            {
                Budget = new Budget
                {
                    BudgetAmount = 100,
                    CurrentSpending = 150,
                    LimitAction = "block"
                },
                IsOverBudget = true,
                RemainingBudget = 0,
                ShouldBlock = true
            });

        // Act
        var result = await _service.CheckAndEnforceAsync(apiKey, "test-model");

        // Assert
        result.IsAllowed.Should().BeFalse();
        result.Reason.Should().Be("Budget exceeded");
        
        _mockWebhookService.Verify(x => x.SendBudgetExceededAsync(
            apiKey.Id.ToString(), "ApiKey", It.IsAny<Budget>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckAndEnforceAsync_WithTeam_ShouldCheckTeamBudget()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            KeyHash = "test-hash",
            Permissions = "*",
            IsActive = true,
            TeamId = teamId
        };

        _mockBudgetService.Setup(x => x.CheckBudgetAsync(
                teamId.ToString(), "Team", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BudgetCheckResult
            {
                Budget = new Budget
                {
                    BudgetAmount = 1000,
                    CurrentSpending = 100,
                    LimitAction = "warn"
                },
                IsOverBudget = false,
                RemainingBudget = 900,
                ShouldBlock = false
            });

        // Act
        var result = await _service.CheckAndEnforceAsync(apiKey, "test-model");

        // Assert
        result.IsAllowed.Should().BeTrue();
        result.RemainingBudget.Should().Be(900);
        
        // Verify it checked team budget, not API key budget
        _mockBudgetService.Verify(x => x.CheckBudgetAsync(
            teamId.ToString(), "Team", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RecordSuccessAsync_ShouldIncrementRateLimitsAndUpdateBudget()
    {
        // Arrange
        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            KeyHash = "test-hash",
            Permissions = "*",
            IsActive = true
        };
        
        var cost = 0.50m;
        var tokenCount = 1000;

        // Act
        await _service.RecordSuccessAsync(
            apiKey, "test-model", "openai", tokenCount, cost);

        // Assert
        _mockRateLimitService.Verify(x => x.IncrementRequestAsync(
            apiKey.KeyHash, tokenCount, It.IsAny<CancellationToken>()),
            Times.Once);
        
        _mockBudgetService.Verify(x => x.UpdateSpendingAsync(
            apiKey.Id.ToString(), "ApiKey", cost, It.IsAny<CancellationToken>()),
            Times.Once);
        
        _mockWebhookService.Verify(x => x.SendRequestSuccessAsync(
            apiKey.KeyHash, "test-model", "openai", tokenCount, cost, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RecordSuccessAsync_WithTeam_ShouldUpdateTeamBudget()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            KeyHash = "test-hash",
            Permissions = "*",
            IsActive = true,
            TeamId = teamId
        };
        
        var cost = 0.50m;

        // Act
        await _service.RecordSuccessAsync(
            apiKey, "test-model", "openai", 1000, cost);

        // Assert - Should update team budget, not API key budget
        _mockBudgetService.Verify(x => x.UpdateSpendingAsync(
            teamId.ToString(), "Team", cost, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RecordSuccessAsync_WithoutCost_ShouldNotUpdateBudget()
    {
        // Arrange
        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            KeyHash = "test-hash",
            Permissions = "*",
            IsActive = true
        };

        // Act
        await _service.RecordSuccessAsync(
            apiKey, "test-model", "ollama", 1000, null);

        // Assert
        // Budget should not be updated when cost is null
        _mockBudgetService.Verify(x => x.UpdateSpendingAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RecordErrorAsync_ShouldSendWebhook()
    {
        // Arrange
        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            KeyHash = "test-hash",
            Permissions = "*",
            IsActive = true
        };
        
        var error = new HttpRequestException("Provider error");

        // Act
        await _service.RecordErrorAsync(
            apiKey, "test-model", error);

        // Assert
        _mockWebhookService.Verify(x => x.SendRequestErrorAsync(
            apiKey.KeyHash, "test-model", error.Message, "HttpRequestException",
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RecordSuccessAsync_WithErrorInBudgetUpdate_ShouldNotThrow()
    {
        // Arrange
        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            KeyHash = "test-hash",
            Permissions = "*",
            IsActive = true
        };
        
        _mockBudgetService.Setup(x => x.UpdateSpendingAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert - Should not throw even if budget update fails
        await _service.RecordSuccessAsync(
            apiKey, "test-model", "openai", 1000, 0.50m);
        
        // Should still have logged the error
        _mockLogger.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckAndEnforceAsync_WithNullRateLimitConfig_ShouldSkipRateLimitCheck()
    {
        // Arrange
        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            KeyHash = "test-hash",
            Permissions = "*",
            IsActive = true,
            RateLimitConfigJson = null
        };

        // Act
        var result = await _service.CheckAndEnforceAsync(apiKey, "test-model");

        // Assert
        result.IsAllowed.Should().BeTrue();
        
        // Rate limit service should not be called
        _mockRateLimitService.Verify(x => x.CheckRateLimitAsync(
            It.IsAny<string>(), It.IsAny<RateLimitConfig>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
