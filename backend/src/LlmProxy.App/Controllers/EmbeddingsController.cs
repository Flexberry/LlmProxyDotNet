using LlmProxy.Core.Models.Dto;
using Microsoft.AspNetCore.Mvc;

namespace LlmProxy.App.Controllers;

[ApiController]
[Route("v1/[controller]")]
public class EmbeddingsController : ControllerBase
{
    private readonly ILogger<EmbeddingsController> _logger;

    public EmbeddingsController(ILogger<EmbeddingsController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateEmbeddings([FromBody] EmbeddingRequest request)
    {
        // TODO: Реализация через Provider Adapter (этап 5-6)
        
        // Заглушка для этапа 4
        var mockEmbedding = new List<float>(1536); // OpenAI-compatible dimension
        var rnd = new Random();
        for (int i = 0; i < 1536; i++)
            mockEmbedding.Add((float)(rnd.NextDouble() * 2 - 1));

        var response = new EmbeddingResponse
        {
            Model = request.Model,
            Data = new List<EmbeddingData>
            {
                new() { Index = 0, Embedding = mockEmbedding }
            },
            Usage = new Usage { PromptTokens = 5, CompletionTokens = 0, TotalTokens = 5 }
        };

        return Ok(response);
    }
}