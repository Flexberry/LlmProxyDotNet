using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using LlmProxy.Core.Models.Dto;
using LlmProxy.Core.Providers;
using LlmProxy.Core.Router;
using LlmProxy.App.Controllers;
using LlmProxy.Infrastructure.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LlmProxy.Tests.Unit.Controllers;

public class ChatControllerTests
{
    private readonly Mock<ILogger<ChatController>> _mockLogger;
    private readonly Mock<ILlmRouter> _mockRouter;
    private readonly Mock<ProviderFactory> _mockFactory;
    private readonly Mock<Core.Logging.ILoggingService> _mockLoggingService;
    private readonly Mock<Infrastructure.Services.IRateLimitEnforcerService> _mockEnforcerService;
    private readonly ChatController _controller;

    public ChatControllerTests()
    {
        _mockLogger = new Mock<ILogger<ChatController>>();
        _mockRouter = new Mock<ILlmRouter>();
        _mockFactory = new Mock<ProviderFactory>(MockBehavior.Loose, new List<ILlmProvider>());
        _mockLoggingService = new Mock<Core.Logging.ILoggingService>();
        _mockEnforcerService = new Mock<Infrastructure.Services.IRateLimitEnforcerService>();
        
        _controller = new ChatController(
            _mockLogger.Object,
            _mockRouter.Object,
            _mockFactory.Object,
            _mockLoggingService.Object,
            _mockEnforcerService.Object
        );
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.ControllerContext.HttpContext.Response.Body = new MemoryStream();
    }

    [Fact]
    public async Task CreateChatCompletion_ReturnsBadRequest_WhenModelMissing()
    {
        var request = new ChatCompletionRequest { Messages = [new ChatMessage { Role = "user", Content = "Hi" }] };
        var result = await _controller.CreateChatCompletion(request);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateChatCompletion_ReturnsBadRequest_WhenMessagesMissing()
    {
        var request = new ChatCompletionRequest { Model = "openai/gpt-4" };
        var result = await _controller.CreateChatCompletion(request);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateChatCompletion_CallsRouter_WithValidRequest()
    {
        var mockProvider = CreateMockProvider("openai");
        var expectedResponse = new ChatCompletionResponse
        {
            Id = "test-123",
            Model = "openai/gpt-4",
            Choices = [new ChatChoice { Index = 0, Message = new ChatMessage { Role = "assistant", Content = "Hi" }, FinishReason = "stop" }]
        };

        _mockRouter.Setup(r => r.SelectProviderAsync(It.IsAny<string>(), It.IsAny<IEnumerable<ILlmProvider>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockProvider);
        _mockRouter.Setup(r => r.ExecuteWithFallback(It.IsAny<Func<ILlmProvider, CancellationToken, Task<ChatCompletionResponse>>>(), 
            It.IsAny<string>(), It.IsAny<IEnumerable<ILlmProvider>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var request = new ChatCompletionRequest { Model = "openai/gpt-4", Messages = [new ChatMessage { Role = "user", Content = "Hello" }] };
        var result = await _controller.CreateChatCompletion(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ChatCompletionResponse>(okResult.Value);
        Assert.Equal("test-123", response.Id);
    }

    [Fact]
    public async Task CreateChatCompletion_HandlesProviderError_WithFallback()
    {
        _mockRouter.Setup(r => r.ExecuteWithFallback<ChatCompletionResponse>(
                It.IsAny<Func<ILlmProvider, CancellationToken, Task<ChatCompletionResponse>>>(),
                It.IsAny<string>(), It.IsAny<IEnumerable<ILlmProvider>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AggregateException(new HttpRequestException("Service Unavailable", null, HttpStatusCode.ServiceUnavailable)));

        var request = new ChatCompletionRequest { Model = "openai/gpt-4", Messages = [new ChatMessage { Role = "user", Content = "Hello" }] };
        
        var result = await _controller.CreateChatCompletion(request);
        var errorResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(502, errorResult.StatusCode);
    }

    [Fact]
    public async Task CreateChatCompletion_Stream_ReturnsFileStreamResult()
    {
        var request = new ChatCompletionRequest { Model = "openai/gpt-4", Messages = [new ChatMessage { Role = "user", Content = "Hello" }], Stream = true };
        
        var mockProvider = CreateMockProvider("openai");
        _mockRouter.Setup(r => r.SelectProviderAsync(It.IsAny<string>(), It.IsAny<IEnumerable<ILlmProvider>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockProvider);

        var result = await _controller.CreateChatCompletion(request);
        
        Assert.NotNull(result);
        
        var contentType = _controller.ControllerContext.HttpContext.Response.Headers.ContentType;
        Assert.Equal("text/event-stream", contentType);
    }

    private static ILlmProvider CreateMockProvider(string name)
    {
        var mock = new Mock<ILlmProvider>();
        mock.SetupGet(p => p.ProviderName).Returns(name);
        mock.SetupGet(p => p.Prefix).Returns($"{name}/");
        mock.Setup(p => p.CreateChatCompletionAsync(It.IsAny<ChatCompletionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatCompletionResponse { Id = "mock", Model = name, Choices = [] });
        return mock.Object;
    }
}