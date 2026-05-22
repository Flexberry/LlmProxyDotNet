using LlmProxy.Core.Config;
using LlmProxy.Core.Providers;
using Microsoft.Extensions.Logging;

namespace LlmProxy.Infrastructure.Providers.Vllm;

public class VllmAdapter : OpenAI.OpenAIAdapter, ILlmProvider
{
    public new string ProviderName => "vllm";
    public new string Prefix => "vllm/";

    public VllmAdapter(HttpClient httpClient, ProviderSettings settings, ILogger<VllmAdapter> logger) 
        : base(httpClient, settings, logger) { }
}