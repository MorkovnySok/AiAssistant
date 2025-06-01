using AiAssistant.Core.Models;

namespace AiAssistant.Core.Interfaces;

public interface ILLMService
{
    Task<string> GetCompletionAsync(CompletionRequest request, CancellationToken cancellationToken = default);
    Task<float[]> GenerateEmbeddingsAsync(string text, CancellationToken cancellationToken = default);
    Task SwitchModelAsync(string modelName, CancellationToken cancellationToken = default);
} 