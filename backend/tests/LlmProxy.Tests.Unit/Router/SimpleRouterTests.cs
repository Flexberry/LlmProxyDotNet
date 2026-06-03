using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using LlmProxy.Core.Config;
using LlmProxy.Core.Providers;
using LlmProxy.Infrastructure.Router;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace LlmProxy.Tests.Unit.Router;

public class SimpleRouterTests
{
    private readonly SimpleRouter _router;
    private readonly List<ILlmProvider> _providers;

    public SimpleRouterTests()
    {
        var config = Options.Create(new LlmConfig { DefaultProvider = "openai" });
        _router = new SimpleRouter(config, NullLogger<SimpleRouter>.Instance);
        
        var mockOpenAi = new Mock<ILlmProvider>();
        mockOpenAi.SetupGet(p => p.Prefix).Returns("openai/");
        mockOpenAi.SetupGet(p => p.ProviderName).Returns("openai");

        var mockOllama = new Mock<ILlmProvider>();
        mockOllama.SetupGet(p => p.Prefix).Returns("ollama/");
        mockOllama.SetupGet(p => p.ProviderName).Returns("ollama");

        _providers = new List<ILlmProvider> { mockOpenAi.Object, mockOllama.Object };
    }

    [Fact]
    public async Task SelectProviderAsync_SelectsByPrefix()
    {
        var provider = await _router.SelectProviderAsync("ollama/llama3", _providers);
        Assert.Equal("ollama", provider.ProviderName);
    }

    [Fact]
    public async Task SelectProviderAsync_UsesRoundRobinWhenNoPrefix()
    {
        var p1 = await _router.SelectProviderAsync("gpt-4", _providers);
        var p2 = await _router.SelectProviderAsync("gpt-4", _providers);
        var p3 = await _router.SelectProviderAsync("gpt-4", _providers);

        Assert.NotEqual(p1.ProviderName, p2.ProviderName);
        Assert.Equal(p1.ProviderName, p3.ProviderName);
    }

    [Fact]
    public async Task ExecuteWithFallback_TriesNextProviderOnError()
    {
        var providerSequence = new List<string>();
        Task<string> Operation(ILlmProvider p, CancellationToken ct)
        {
            providerSequence.Add(p.ProviderName);
            if (providerSequence.Count == 1) 
                throw new HttpRequestException("Service Unavailable", null, HttpStatusCode.ServiceUnavailable);
            return Task.FromResult("success");
        }

        var result = await _router.ExecuteWithFallback(Operation, "test-model", _providers, maxRetries: 2);

        Assert.Equal("success", result);
        Assert.Equal(2, providerSequence.Count);
        Assert.Equal("openai", providerSequence[0]);
        Assert.Equal("ollama", providerSequence[1]);
    }
}