using LlmProxy.Core.Models.Dto;
using Microsoft.AspNetCore.Mvc;

namespace LlmProxy.App.Controllers;

/// <summary>
/// Controller for handling embedding requests
/// </summary>
[ApiController]
[Route("v1/[controller]")]
public class EmbeddingsController : ControllerBase
{
    private readonly ILogger<EmbeddingsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmbeddingsController"/> class
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public EmbeddingsController(ILogger<EmbeddingsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates embeddings for the given input text
    /// </summary>
    /// <param name="request">Embedding request containing input text and model</param>
    /// <returns>Mock embedding response (placeholder for future implementation)</returns>
    [HttpPost]
    public async Task<IActionResult> CreateEmbeddings([FromBody] EmbeddingRequest request)
    {
        // Placeholder for embeddings - to be implemented in stage 5-6
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