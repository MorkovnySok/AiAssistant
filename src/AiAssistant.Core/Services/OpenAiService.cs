using System.ClientModel;
using AiAssistant.Core.Interfaces;
using AiAssistant.Core.Models;
using Microsoft.SemanticKernel;
using OpenAI;

namespace AiAssistant.Core.Services;

public class OpenAiService : ILLMService
{
    private static readonly OpenAIClient openAiClient = new(
        new ApiKeyCredential(
            Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new ArgumentNullException(
                    null,
                    "OPENAI_API_KEY must be set in the environment variables."
                )
        ),
        new OpenAIClientOptions()
        {
            Endpoint =
                Environment.GetEnvironmentVariable("OPENAI_ENDPOINT") != null
                    ? new Uri(Environment.GetEnvironmentVariable("OPENAI_ENDPOINT")!)
                    : null,
        }
    );
    private readonly Kernel _kernel = Kernel
        .CreateBuilder()
        .AddOpenAIChatClient(
            Environment.GetEnvironmentVariable("OPENAI_MODEL")
                ?? throw new ArgumentNullException(
                    null,
                    "OPENAI_MODEL must be set in the environment variables. More info - https://platform.openai.com/docs/models"
                ),
            openAIClient: openAiClient
        )
        .Build();

    public async Task<string> GetCompletionAsync(
        CompletionRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var result = await _kernel.InvokePromptAsync(
            request.Prompt,
            cancellationToken: cancellationToken
        );
        return result.RenderedPrompt ?? "Sorry, response wasn't generated";
    }

    public async Task<float[]> GenerateEmbeddingsAsync(
        string text,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }
}
