using AiAssistant.Core.Services;
using OllamaSharp;
using OllamaSharp.Models;

namespace AiAssistant.Api.Startup;

public class OllamaBuilder(ConfigurationManager configuration)
{
    public async Task<OllamaService> SetupOllamaService()
    {
        var model = configuration["Ollama:DefaultModel"] ?? "deepseek-r1:8b";
        var ollamaServer = configuration["Ollama:BaseUrl"] ?? "http://ollama:11434";
        Console.WriteLine("Ollama model is " + model);
        Console.WriteLine("Ollama is running at " + ollamaServer);
        var ollamaClient = new OllamaApiClient(ollamaServer);
        var existingModels = await ollamaClient.ListLocalModelsAsync(CancellationToken.None);
        if (existingModels.All(x => x.Name != model))
        {
            Console.WriteLine("Ollama model is not pulled, starting to pull...");
            var i = 0;
            await foreach (var status in ollamaClient.PullModelAsync(model, CancellationToken.None))
            {
                if (i == 20)
                {
                    Console.WriteLine(
                        $"{status!.Percent}% {status.Status}. Pulled {status.Completed / 1024 / 1024} mb out of {status.Total / 1024 / 1024} mb"
                    );
                    i = 0;
                }
                i++;
            }
        }

        await foreach (
            var i in ollamaClient.GenerateAsync(
                new GenerateRequest()
                {
                    Prompt = "this is a warm up request",
                    Stream = false,
                    Model = model,
                }
            )
        )
        {
            Console.WriteLine("Warm up request generated: " + i!.Response);
        }

        return new OllamaService(model, ollamaServer);
    }
}
