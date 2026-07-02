using System.Net.Http.Headers;
using LlmProxy.Core.Config;
using LlmProxy.Core.Providers;
using LlmProxy.Infrastructure.Providers.OpenAI;
using Microsoft.Extensions.Logging;

namespace LlmProxy.Infrastructure.Providers.OpenRouter;

public class OpenRouterAdapter : OpenAIAdapter, ILlmProvider
{
    public override string ProviderName => "openrouter";
    public override string Prefix => "openrouter/";

    public OpenRouterAdapter(HttpClient httpClient, ProviderSettings settings, ILogger<OpenRouterAdapter> logger) 
        : base(httpClient, settings, logger)
    {
        // OpenRouter требует дополнительные заголовки
        _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://llmproxy.local");
        _httpClient.DefaultRequestHeaders.Add("X-Title", "LlmProxyDotNet");
    }
}