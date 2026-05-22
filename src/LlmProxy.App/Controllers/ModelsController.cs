using Microsoft.AspNetCore.Mvc;
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
    // TODO: Inject IOptions<LlmConfig> для чтения конфигурации провайдеров

    [HttpGet]
    public IActionResult ListModels()
    {
        // Заглушка: возвращаем статический список для этапа 4
        // В этапе 6 будет агрегация из ProviderConfig + кэширование
        var models = new List<ModelInfo>
        {
            new() { Id = "openai/gpt-4o", OwnedBy = "openai" },
            new() { Id = "openai/gpt-4o-mini", OwnedBy = "openai" },
            new() { Id = "ollama/llama3", OwnedBy = "ollama" },
            new() { Id = "ollama/mistral", OwnedBy = "ollama" },
            new() { Id = "vllm/mistral-7b", OwnedBy = "vllm" },
            new() { Id = "openrouter/meta-llama/llama-3-70b-instruct", OwnedBy = "openrouter" },
            new() { Id = "zai/z-ai-chat", OwnedBy = "zai" }
        };

        return Ok(new ModelsListResponse { Data = models });
    }
}