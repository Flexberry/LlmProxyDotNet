using LlmProxy.Core.Providers;
using LlmProxy.Infrastructure.Providers;
using Moq;
using Xunit;

namespace LlmProxy.Tests.Unit.Providers;

public class ProviderFactoryTests
{
    [Fact]
    public void GetByPrefix_ReturnsCorrectProvider()
    {
        // Arrange
        var providers = new List<ILlmProvider>
        {
            CreateMockProvider("openai", "openai/"),
            CreateMockProvider("ollama", "ollama/"),
            CreateMockProvider("vllm", "vllm/")
        };
        var factory = new ProviderFactory(providers);

        // Act
        var result = factory.GetByPrefix("ollama/llama3");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ollama", result!.ProviderName);
    }

    [Fact]
    public void GetByPrefix_ReturnsNullForUnknownPrefix()
    {
        // Arrange
        var providers = new List<ILlmProvider> { CreateMockProvider("openai", "openai/") };
        var factory = new ProviderFactory(providers);

        // Act
        var result = factory.GetByPrefix("unknown/model");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetAll_ReturnsAllRegisteredProviders()
    {
        // Arrange
        var providers = new List<ILlmProvider>
        {
            CreateMockProvider("openai", "openai/"),
            CreateMockProvider("ollama", "ollama/")
        };
        var factory = new ProviderFactory(providers);

        // Act
        var result = factory.GetAll().ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.ProviderName == "openai");
        Assert.Contains(result, p => p.ProviderName == "ollama");
    }

    [Theory]
    [InlineData("OPENAI/gpt-4", "openai")]
    [InlineData("Ollama/llama3", "ollama")]
    [InlineData("VLLM/mistral", "vllm")]
    public void GetByPrefix_IsCaseInsensitive(string modelName, string expectedProvider)
    {
        // Arrange
        var providers = new List<ILlmProvider>
        {
            CreateMockProvider("openai", "openai/"),
            CreateMockProvider("ollama", "ollama/"),
            CreateMockProvider("vllm", "vllm/")
        };
        var factory = new ProviderFactory(providers);

        // Act
        var result = factory.GetByPrefix(modelName);

        // Assert
        Assert.Equal(expectedProvider, result?.ProviderName);
    }

    private static ILlmProvider CreateMockProvider(string name, string prefix)
    {
        var mock = new Mock<ILlmProvider>();
        mock.SetupGet(p => p.ProviderName).Returns(name);
        mock.SetupGet(p => p.Prefix).Returns(prefix);
        return mock.Object;
    }
}