using LlmProxy.Core.Config;
using LlmProxy.Core.Providers;
using LlmProxy.Infrastructure.Providers.OpenAI;
using Microsoft.Extensions.Logging;

namespace LlmProxy.Infrastructure.Providers.ZAi;

public class ZAiAdapter : OpenAIAdapter, ILlmProvider
{
    public new string ProviderName => "zai";
    public new string Prefix => "zai/";

    public ZAiAdapter(HttpClient httpClient, ProviderSettings settings, ILogger<ZAiAdapter> logger) 
        : base(httpClient, settings, logger) { }
}