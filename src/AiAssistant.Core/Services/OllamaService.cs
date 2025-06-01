using System.Text;
using System.Text.Json;
using AiAssistant.Core.Interfaces;
using AiAssistant.Core.Models;

namespace AiAssistant.Core.Services;

public class OllamaService : ILLMService
{
    private readonly HttpClient _httpClient;
    private string _currentModel = "llama2";

    public OllamaService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetCompletionAsync(CompletionRequest request, CancellationToken cancellationToken = default)
    {
        var model = request.ModelConfig?.ModelName ?? _currentModel;
        var endpoint = request.ModelConfig?.EndpointUrl ?? "http://localhost:11434";

        var requestData = new
        {
            model = model,
            prompt = request.Prompt,
            stream = false,
            options = request.ModelConfig?.Parameters ?? new Dictionary<string, string>()
        };

        var response = await _httpClient.PostAsync(
            $"{endpoint}/api/generate",
            new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json"),
            cancellationToken);

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<OllamaResponse>(content);

        return result?.Response ?? string.Empty;
    }

    public async Task<float[]> GenerateEmbeddingsAsync(string text, CancellationToken cancellationToken = default)
    {
        var requestData = new
        {
            model = _currentModel,
            prompt = text
        };

        var response = await _httpClient.PostAsync(
            "http://localhost:11434/api/embeddings",
            new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json"),
            cancellationToken);

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<OllamaEmbeddingResponse>(content);

        return result?.Embedding ?? Array.Empty<float>();
    }

    public Task SwitchModelAsync(string modelName, CancellationToken cancellationToken = default)
    {
        _currentModel = modelName;
        return Task.CompletedTask;
    }

    private class OllamaResponse
    {
        public string Response { get; set; } = string.Empty;
    }

    private class OllamaEmbeddingResponse
    {
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
} 