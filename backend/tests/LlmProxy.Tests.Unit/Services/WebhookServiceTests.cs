using FluentAssertions;
using LlmProxy.Core.Entities;
using LlmProxy.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;

namespace LlmProxy.Tests.Unit.Services;

public class WebhookServiceTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<WebhookService>> _loggerMock;
    private readonly WebhookService _service;

    public WebhookServiceTests()
    {
        _httpHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpHandlerMock.Object) { BaseAddress = new Uri("http://localhost") };
        _loggerMock = new Mock<ILogger<WebhookService>>();
        _service = new WebhookService(_httpClient, _loggerMock.Object, Mock.Of<IServiceProvider>());
    }

    [Fact]
    public async Task SendWebhookAsync_WhenSuccess_ShouldReturnTrue()
    {
        // Arrange
        var webhookUrl = "http://localhost:9000/webhook";
        
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        // Act
        var result = await _service.SendWebhookAsync(webhookUrl, WebhookEventType.RequestSuccess, new { test = "data" });

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendWebhookAsync_WhenFailure_ShouldReturnFalse()
    {
        // Arrange
        var webhookUrl = "http://localhost:9000/webhook";
        
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Act
        var result = await _service.SendWebhookAsync(webhookUrl, WebhookEventType.RequestError, new { error = "test" });

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendWebhookAsync_WhenException_ShouldReturnFalseAndLogError()
    {
        // Arrange
        var webhookUrl = "http://invalid-url";
        
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        // Act
        var result = await _service.SendWebhookAsync(webhookUrl, WebhookEventType.BudgetExceeded, new { budget = 100 });

        // Assert
        result.Should().BeFalse();
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendWebhookAsync_ShouldUsePostMethod()
    {
        // Arrange
        var webhookUrl = "http://localhost:9000/webhook";
        
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

        // Act
        await _service.SendWebhookAsync(webhookUrl, WebhookEventType.RateLimitExceeded, new { apiKeyHash = "test-key" });

        // Assert - Setup verifies POST method was used
        _httpHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendRequestSuccessAsync_ShouldCallSendWebhook()
    {
        // Arrange
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

        // Act
        await _service.SendRequestSuccessAsync("api-key-hash", "gpt-4", "openai", 1000, 0.50m);

        // Assert
        _httpHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendRequestErrorAsync_ShouldCallSendWebhook()
    {
        // Arrange
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

        // Act
        await _service.SendRequestErrorAsync("api-key-hash", "gpt-4", "Timeout error", "TimeoutException");

        // Assert
        _httpHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendRateLimitExceededAsync_ShouldCallSendWebhook()
    {
        // Arrange
        var rateLimitConfig = new RateLimitConfig { RequestsPerMinute = 100 };
        
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

        // Act
        await _service.SendRateLimitExceededAsync("api-key-hash", "gpt-4", rateLimitConfig);

        // Assert
        _httpHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendRateLimitExceededAsync_WithNullConfig_ShouldUseDefaultLimit()
    {
        // Arrange
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

        // Act
        await _service.SendRateLimitExceededAsync("api-key-hash", "gpt-4", null);

        // Assert - Should not throw even with null config
        _httpHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendBudgetExceededAsync_ShouldCallSendWebhook()
    {
        // Arrange
        var budget = new Budget
        {
            BudgetAmount = 1000,
            CurrentSpending = 1100,
            LimitAction = "block"
        };
        
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

        // Act
        await _service.SendBudgetExceededAsync("entity-123", "ApiKey", budget);

        // Assert
        _httpHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendBudgetExceededAsync_WithNullBudget_ShouldUseDefaults()
    {
        // Arrange
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

        // Act
        await _service.SendBudgetExceededAsync("entity-123", "ApiKey", null);

        // Assert - Should not throw even with null budget
        _httpHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendRequestSuccessWithUrlAsync_ShouldReturnTrueOnSuccess()
    {
        // Arrange
        var webhookUrl = "http://localhost:9000/webhook";
        
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

        // Act
        var result = await _service.SendRequestSuccessWithUrlAsync(
            webhookUrl, "request-123", "gpt-4", "openai", 1000, 0.50m);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendRequestErrorWithUrlAsync_ShouldReturnTrueOnSuccess()
    {
        // Arrange
        var webhookUrl = "http://localhost:9000/webhook";
        
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

        // Act
        var result = await _service.SendRequestErrorWithUrlAsync(
            webhookUrl, "request-123", "gpt-4", "Connection timeout");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendRateLimitExceededWithUrlAsync_ShouldReturnTrueOnSuccess()
    {
        // Arrange
        var webhookUrl = "http://localhost:9000/webhook";
        
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

        // Act
        var result = await _service.SendRateLimitExceededWithUrlAsync(
            webhookUrl, "api-key-hash", "requests_per_minute", 150, 100);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendBudgetExceededWithUrlAsync_ShouldReturnTrueOnSuccess()
    {
        // Arrange
        var webhookUrl = "http://localhost:9000/webhook";
        
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

        // Act
        var result = await _service.SendBudgetExceededWithUrlAsync(
            webhookUrl, "entity-123", "ApiKey", 1100, 1000);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendWebhookAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var webhookUrl = "http://invalid-url";
        var cts = new CancellationTokenSource();
        cts.Cancel();
        
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException());

        // Act
        var result = await _service.SendWebhookAsync(webhookUrl, WebhookEventType.RequestSuccess, new { }, cts.Token);

        // Assert
        result.Should().BeFalse();
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

