using System.Text;
using AiAssistant.Core.Interfaces;
using AiAssistant.Core.Models;
using OllamaSharp;
using OllamaSharp.Models;

namespace AiAssistant.Core.Services;

public class OllamaService : ILLMService
{
    private readonly OllamaApiClient _ollamaClient;
    private readonly string _currentModel;

    public OllamaService(string currentModel, string ollamaApi)
    {
        _currentModel = currentModel;
        _ollamaClient = new OllamaApiClient(new Uri(ollamaApi));
        _ollamaClient.SelectedModel = _currentModel;
    }

    public async Task<string> GetCompletionAsync(
        CompletionRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var completionRequest = new GenerateRequest
        {
            Model = _currentModel,
            Prompt = request.Prompt,
            Stream = true,
        };

        var responseBuilder = new StringBuilder();

        await foreach (
            var stream in _ollamaClient.GenerateAsync(completionRequest, cancellationToken)
        )
        {
            if (stream != null && !string.IsNullOrEmpty(stream.Response))
            {
                Console.Write(stream.Response);
                responseBuilder.Append(stream.Response);
            }
        }

        return responseBuilder.ToString();
    }

    public async Task<float[]> GenerateEmbeddingsAsync(
        string text,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _ollamaClient.EmbedAsync(text, cancellationToken);
        return response.Embeddings.First();
    }
}
