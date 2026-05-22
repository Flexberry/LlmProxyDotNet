using LlmProxy.Core.Models.Dto;

namespace LlmProxy.Core.Providers;

public interface ILlmProvider
{
    string ProviderName { get; }
    string Prefix { get; }
    
    Task<ChatCompletionResponse> CreateChatCompletionAsync(
        ChatCompletionRequest request, 
        CancellationToken ct = default);
    
    IAsyncEnumerable<ChatCompletionChunk> CreateChatCompletionStreamAsync(
        ChatCompletionRequest request, 
        CancellationToken ct = default);
    
    Task<EmbeddingResponse> CreateEmbeddingsAsync(
        EmbeddingRequest request, 
        CancellationToken ct = default);
}