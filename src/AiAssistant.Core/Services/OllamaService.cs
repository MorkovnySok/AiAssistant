using AiAssistant.Core.Interfaces;
using AiAssistant.Core.Models;
using OllamaSharp;
using OllamaSharp.Models;
using System.Text;

namespace AiAssistant.Core.Services;

public class OllamaService : ILLMService
{
    private readonly OllamaApiClient _ollamaClient;
    private string _currentModel = "deepseek-r1:8b";

    public OllamaService()
    {
        _ollamaClient = new OllamaApiClient(new Uri("http://localhost:11434"));
        _ollamaClient.SelectedModel =  _currentModel;
        var models = _ollamaClient.ListLocalModelsAsync(CancellationToken.None).Result;
        if (models.All(x => x.Name != _currentModel))
        {
            _ollamaClient.PullModelAsync(new PullModelRequest() { Insecure = true, Model = _currentModel });
        }
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
            Stream = true
        };

        var responseBuilder = new StringBuilder();

        await foreach (var stream in _ollamaClient.GenerateAsync(completionRequest, cancellationToken))
        {
            if (stream != null && !string.IsNullOrEmpty(stream.Response))
            {
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
    
    public Task SwitchModelAsync(string modelName)
    {
        _currentModel = modelName;
        return Task.CompletedTask;
    }
}
