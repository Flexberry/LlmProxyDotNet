using System.Net;
using System.Text;
using System.Text.Json;
using LlmProxy.Core.Config;
using LlmProxy.Core.Models.Dto;
using LlmProxy.Infrastructure.Providers.Ollama;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace LlmProxy.Tests.Unit.Providers;

public class OllamaAdapterTests
{
    private readonly ProviderSettings _settings;
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly HttpClient _httpClient;

    public OllamaAdapterTests()
    {
        _settings = new ProviderSettings
        {
            BaseUrl = "http://localhost:11434",
            ApiKey = null,
            Prefix = "ollama/"
        };
        _mockHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHandler.Object) { BaseAddress = new Uri(_settings.BaseUrl) };
    }

    [Fact]
    public async Task CreateChatCompletionAsync_MapsOpenAIRequestToOllamaFormat()
    {
        // Arrange
        var ollamaResponse = new OllamaChatResponse(
            Model: "llama3",
            Message: new OllamaMessage("assistant", "Hello from Ollama!"),
            Done: true
        );

        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => 
                    r.RequestUri != null &&
                    r.RequestUri.ToString().Contains("/api/chat") == true &&
                    r.Content != null),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(ollamaResponse), Encoding.UTF8, "application/json")
            });

        var adapter = new OllamaAdapter(_httpClient, _settings, NullLogger<OllamaAdapter>.Instance);
        var request = new ChatCompletionRequest
        {
            Model = "ollama/llama3",
            Messages = [new ChatMessage { Role = "user", Content = "Hi" }]
        };

        // Act
        var result = await adapter.CreateChatCompletionAsync(request);

        // Assert
        Assert.Equal("Hello from Ollama!", result.Choices[0].Message.Content);
        Assert.Equal("ollama/llama3", result.Model);
    }

    [Fact]
    public async Task CreateChatCompletionStreamAsync_ParsesOllamaStream()
    {
        // Arrange
        var streamLines = new[]
        {
            """{"model":"llama3","message":{"role":"assistant","content":"Hello"},"done":false}""",
            """{"model":"llama3","message":{"role":"assistant","content":" from"},"done":false}""",
            """{"model":"llama3","message":{"role":"assistant","content":" Ollama!"},"done":true}"""
        };
        var streamContent = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(string.Join("\n", streamLines))));

        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = streamContent });

        var adapter = new OllamaAdapter(_httpClient, _settings, NullLogger<OllamaAdapter>.Instance);
        var request = new ChatCompletionRequest { Model = "ollama/llama3", Messages = [new ChatMessage { Role = "user", Content = "Hi" }], Stream = true };
        
        // Act
        var chunks = new List<ChatCompletionChunk>();
        await foreach (var chunk in adapter.CreateChatCompletionStreamAsync(request))
        {
            chunks.Add(chunk);
        }

        // Assert
        Assert.Equal(3, chunks.Count);
        Assert.Contains("Hello", chunks[0].Choices[0].Delta.Content);
        Assert.Equal("stop", chunks[2].Choices[0].FinishReason);
    }

    [Fact]
    public void StripPrefix_RemovesOllamaPrefix()
    {
        // Arrange & Act
        var adapter = new OllamaAdapter(_httpClient, _settings, NullLogger<OllamaAdapter>.Instance);
        
        var method = typeof(OllamaAdapter).GetMethod("StripPrefix", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = method?.Invoke(adapter, new object[] { "ollama/llama3" });

        // Assert
        Assert.Equal("llama3", result);
    }

    [Fact]
    public async Task CreateEmbeddingsAsync_ReturnsEmbeddings()
    {
        // Arrange
        var embeddingsResponse = new
        {
            embeddings = new List<List<float>> { new List<float> { 0.1f, 0.2f, 0.3f } },
            total_token_count = 10
        };

        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => 
                    r.RequestUri != null &&
                    r.RequestUri.ToString().Contains("/api/embed")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(embeddingsResponse), Encoding.UTF8, "application/json")
            });

        var adapter = new OllamaAdapter(_httpClient, _settings, NullLogger<OllamaAdapter>.Instance);
        var request = new EmbeddingRequest
        {
            Model = "ollama/llama3",
            Input = new List<string> { "Hello world" }
        };

        // Act
        var result = await adapter.CreateEmbeddingsAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Data.Count);
        Assert.Equal(3, result.Data[0].Embedding.Count);
        Assert.Equal(10, result.Usage.PromptTokens);
    }

    [Fact]
    public async Task CreateChatCompletionAsync_HandlesProviderError()
    {
        // Arrange
        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError });

        var adapter = new OllamaAdapter(_httpClient, _settings, NullLogger<OllamaAdapter>.Instance);
        var request = new ChatCompletionRequest
        {
            Model = "ollama/llama3",
            Messages = [new ChatMessage { Role = "user", Content = "Hi" }]
        };

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(async () =>
            await adapter.CreateChatCompletionAsync(request));
    }

    [Fact]
    public async Task CreateChatCompletionStreamAsync_HandlesStreamError()
    {
        // Arrange
        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.ServiceUnavailable });

        var adapter = new OllamaAdapter(_httpClient, _settings, NullLogger<OllamaAdapter>.Instance);
        var request = new ChatCompletionRequest
        {
            Model = "ollama/llama3",
            Messages = [new ChatMessage { Role = "user", Content = "Hi" }],
            Stream = true
        };

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await foreach (var _ in adapter.CreateChatCompletionStreamAsync(request)) { }
        });
    }
}