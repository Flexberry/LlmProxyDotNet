using System.Security.Claims;
using LlmProxy.Core.Auth;
using LlmProxy.Core.Entities;
using LlmProxy.Core.Utils;
using LlmProxy.App.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace LlmProxy.Tests.Unit.Auth;

public class ApiKeyAuthMiddlewareTests
{
    private readonly DefaultHttpContext _context;
    private readonly Mock<IApiKeyStore> _mockStore;
    private readonly ApiKeyAuthMiddleware _middleware;

    public ApiKeyAuthMiddlewareTests()
    {
        _context = new DefaultHttpContext();
        _mockStore = new Mock<IApiKeyStore>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["LITELLM_MASTER_KEY"] = "sk_master_test" })
            .Build();
        
        _middleware = new ApiKeyAuthMiddleware(_ => Task.CompletedTask, config);
    }

    [Fact]
    public async Task InvokeAsync_AllowsMasterKey()
    {
        // Arrange
        _context.Request.Headers["X-Admin-Key"] = "sk_master_test";
        _context.Request.Path = "/admin/keys";

        // Act
        await _middleware.InvokeAsync(_context, _mockStore.Object);

        // Assert
        Assert.Equal(200, _context.Response.StatusCode); // Не 401
        Assert.True(_context.Items.ContainsKey("IsMaster"));
    }

    [Fact]
    public async Task InvokeAsync_RejectsMissingAuthHeader()
    {
        // Arrange
        _context.Request.Path = "/v1/chat/completions";

        // Act
        await _middleware.InvokeAsync(_context, _mockStore.Object);

        // Assert
        Assert.Equal(401, _context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_RejectsExpiredKey()
    {
        // Arrange
        var key = "sk_test_expired";
        var hash = KeyHelper.HashKey(key);
        _context.Request.Headers.Authorization = $"Bearer {key}";
        _context.Request.Path = "/v1/chat/completions";

        _mockStore.Setup(s => s.GetByKeyHashAsync(hash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiKey { KeyHash = hash, ExpiresAt = DateTime.UtcNow.AddDays(-1), IsActive = true });

        // Act
        await _middleware.InvokeAsync(_context, _mockStore.Object);

        // Assert
        Assert.Equal(401, _context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_AllowsValidKey()
    {
        // Arrange
        var key = "sk_test_valid";
        var hash = KeyHelper.HashKey(key);
        _context.Request.Headers.Authorization = $"Bearer {key}";
        _context.Request.Path = "/v1/chat/completions";

        _mockStore.Setup(s => s.GetByKeyHashAsync(hash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiKey { KeyHash = hash, IsActive = true, Permissions = "*" });

        // Act
        await _middleware.InvokeAsync(_context, _mockStore.Object);

        // Assert
        Assert.Equal(200, _context.Response.StatusCode); // Пропущен дальше по пайплайну
        Assert.Equal(hash, _context.Items["ApiKeyHash"]);
    }
}