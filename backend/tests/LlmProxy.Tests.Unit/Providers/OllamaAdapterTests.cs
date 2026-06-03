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
}