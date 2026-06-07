using System.Net;
using System.Text;
using System.Text.Json;
using LlmProxy.Core.Config;
using LlmProxy.Core.Models.Dto;
using LlmProxy.Infrastructure.Providers.OpenAI;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace LlmProxy.Tests.Unit.Providers;

public class OpenAIAdapterTests
{
    private readonly ProviderSettings _settings;
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly HttpClient _httpClient;

    public OpenAIAdapterTests()
    {
        _settings = new ProviderSettings
        {
            BaseUrl = "https://api.openai.com/v1",
            ApiKey = "test-key",
            Prefix = "openai/"
        };
        _mockHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHandler.Object) { BaseAddress = new Uri(_settings.BaseUrl) };
    }

    [Fact]
    public async Task CreateChatCompletionAsync_MapsRequestAndReturnsResponse()
    {
        // Arrange
        var mockResponse = new ChatCompletionResponse
        {
            Id = "test-id",
            Model = "gpt-4o",
            Choices = [new ChatChoice { Index = 0, Message = new ChatMessage { Role = "assistant", Content = "Hello" }, FinishReason = "stop" }],
            Usage = new Usage { PromptTokens = 10, CompletionTokens = 5, TotalTokens = 15 }
        };

        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri != null && r.RequestUri.ToString().Contains("/chat/completions")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(mockResponse), Encoding.UTF8, "application/json")
            });

        var adapter = new OpenAIAdapter(_httpClient, _settings, NullLogger<OpenAIAdapter>.Instance);
        var request = new ChatCompletionRequest
        {
            Model = "openai/gpt-4o",
            Messages = [new ChatMessage { Role = "user", Content = "Hi" }]
        };

        // Act
        var result = await adapter.CreateChatCompletionAsync(request);

        // Assert
        Assert.Equal("test-id", result.Id);
        Assert.Equal("Hello", result.Choices[0].Message.Content);
        Assert.Equal(15, result.Usage?.TotalTokens);
    }

    [Fact]
    public async Task CreateChatCompletionStreamAsync_ParsesSseChunks()
    {
        // Arrange
        var sseData = new[]
        {
            "data: {\"id\":\"c1\",\"object\":\"chat.completion.chunk\",\"choices\":[{\"index\":0,\"delta\":{\"role\":\"assistant\"}}]}",
            "data: {\"id\":\"c1\",\"object\":\"chat.completion.chunk\",\"choices\":[{\"index\":0,\"delta\":{\"content\":\"Hello\"}}]}",
            "data: {\"id\":\"c1\",\"object\":\"chat.completion.chunk\",\"choices\":[{\"index\":0,\"delta\":{},\"finish_reason\":\"stop\"}]}",
            "data: [DONE]"
        };

        var streamContent = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(string.Join("\n\n", sseData))));
        
        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = streamContent
            });

        var adapter = new OpenAIAdapter(_httpClient, _settings, NullLogger<OpenAIAdapter>.Instance);
        var request = new ChatCompletionRequest { Model = "openai/gpt-4o", Messages = [new ChatMessage { Role = "user", Content = "Hi" }], Stream = true };

        // Act
        var chunks = new List<ChatCompletionChunk>();
        await foreach (var chunk in adapter.CreateChatCompletionStreamAsync(request))
        {
            chunks.Add(chunk);
        }

        // Assert
        Assert.Equal(3, chunks.Count);
        Assert.Equal("assistant", chunks[0].Choices[0].Delta.Role);
        Assert.Equal("Hello", chunks[1].Choices[0].Delta.Content);
        Assert.Equal("stop", chunks[2].Choices[0].FinishReason);
    }

    [Fact]
    public async Task CreateChatCompletionAsync_ThrowsOnProviderError()
    {
        // Arrange
        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.ServiceUnavailable });

        var adapter = new OpenAIAdapter(_httpClient, _settings, NullLogger<OpenAIAdapter>.Instance);
        var request = new ChatCompletionRequest { Model = "openai/gpt-4o", Messages = [new ChatMessage { Role = "user", Content = "Hi" }] };

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => adapter.CreateChatCompletionAsync(request));
    }

    [Fact]
    public async Task CreateEmbeddingsAsync_ReturnsEmbeddings()
    {
        // Arrange
        var embeddingsResponse = new EmbeddingResponse
        {
            Model = "text-embedding-ada-002",
            Data = [new EmbeddingData { Index = 0, Embedding = new List<float> { 0.1f, 0.2f, 0.3f } }],
            Usage = new Usage { PromptTokens = 5, TotalTokens = 5 }
        };

        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri != null && r.RequestUri.ToString().Contains("/embeddings")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(embeddingsResponse), Encoding.UTF8, "application/json")
            });

        var adapter = new OpenAIAdapter(_httpClient, _settings, NullLogger<OpenAIAdapter>.Instance);
        var request = new EmbeddingRequest
        {
            Model = "openai/text-embedding-ada-002",
            Input = new List<string> { "Hello world" }
        };

        // Act
        var result = await adapter.CreateEmbeddingsAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Data.Count);
        Assert.Equal(3, result.Data[0].Embedding.Count);
    }

    [Fact]
    public void ProviderName_ReturnsOpenai()
    {
        // Arrange
        var adapter = new OpenAIAdapter(_httpClient, _settings, NullLogger<OpenAIAdapter>.Instance);

        // Act & Assert
        Assert.Equal("openai", adapter.ProviderName);
        Assert.Equal("openai/", adapter.Prefix);
    }
}