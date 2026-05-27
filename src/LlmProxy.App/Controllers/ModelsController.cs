using LlmProxy.Core.Config;
using LlmProxy.Infrastructure.Providers.Ollama;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;

namespace LlmProxy.App.Controllers;

public record ModelInfo
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;
    
    [JsonPropertyName("object")]
    public string Object { get; init; } = "model";
    
    [JsonPropertyName("created")]
    public long Created { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    
    [JsonPropertyName("owned_by")]
    public string OwnedBy { get; init; } = "llm-proxy";
}

public record ModelsListResponse
{
    [JsonPropertyName("object")]
    public string Object { get; init; } = "list";
    
    [JsonPropertyName("data")]
    public IReadOnlyList<ModelInfo> Data { get; init; } = Array.Empty<ModelInfo>();
}

[ApiController]
[Route("v1/[controller]")]
public class ModelsController : ControllerBase
{
    private readonly IOptions<LlmConfig> _config;
    private readonly OllamaModelService? _ollamaModelService;
    private readonly ILogger<ModelsController> _logger;

    public ModelsController(
        IOptions<LlmConfig> config,
        OllamaModelService? ollamaModelService,
        ILogger<ModelsController> logger)
    {
        _config = config;
        _ollamaModelService = ollamaModelService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> ListModels()
    {
        var models = new List<ModelInfo>();
        var config = _config.Value;

        // Добавляем модели из каждого провайдера
        foreach (var (key, settings) in config.Providers)
        {
            if (key == "ollama" && _ollamaModelService != null)
            {
                // Динамически получаем модели из Ollama
                try
                {
                    var ollamaModels = await _ollamaModelService.GetAvailableModelsAsync();
                    foreach (var modelName in ollamaModels)
                    {
                        models.Add(new ModelInfo 
                        { 
                            Id = $"ollama/{modelName}", 
                            OwnedBy = "ollama" 
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch Ollama models");
                    // Fallback: добавляем стандартные модели
                    models.Add(new ModelInfo { Id = "ollama/llama3.2", OwnedBy = "ollama" });
                    models.Add(new ModelInfo { Id = "ollama/mistral", OwnedBy = "ollama" });
                }
            }
            else if (!string.IsNullOrEmpty(settings.Prefix))
            {
                // Для других провайдеров используем статические модели из конфига
                AddStaticModelsForProvider(models, key, settings.Prefix);
            }
        }

        return Ok(new ModelsListResponse { Data = models });
    }

    private void AddStaticModelsForProvider(List<ModelInfo> models, string providerKey, string prefix)
    {
        // Статические модели для провайдеров без динамического обнаружения
        switch (providerKey)
        {
            case "openai":
                models.Add(new ModelInfo { Id = $"{prefix}gpt-4o", OwnedBy = "openai" });
                models.Add(new ModelInfo { Id = $"{prefix}gpt-4o-mini", OwnedBy = "openai" });
                models.Add(new ModelInfo { Id = $"{prefix}gpt-3.5-turbo", OwnedBy = "openai" });
                break;
            case "vllm":
                models.Add(new ModelInfo { Id = $"{prefix}mistral-7b", OwnedBy = "vllm" });
                models.Add(new ModelInfo { Id = $"{prefix}llama-3", OwnedBy = "vllm" });
                break;
            case "openrouter":
                models.Add(new ModelInfo { Id = $"{prefix}meta-llama/llama-3-70b-instruct", OwnedBy = "openrouter" });
                break;
            case "zai":
                models.Add(new ModelInfo { Id = $"{prefix}z-ai-chat", OwnedBy = "zai" });
                break;
        }
    }
}