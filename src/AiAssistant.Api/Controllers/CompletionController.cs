using AiAssistant.Core.Interfaces;
using AiAssistant.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace AiAssistant.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompletionController : ControllerBase
{
    private readonly ILLMService _llmService;
    private readonly IVectorStore _vectorStore;

    public CompletionController(ILLMService llmService, IVectorStore vectorStore)
    {
        _llmService = llmService;
        _vectorStore = vectorStore;
    }

    [HttpGet("test")]
    public string GetTest()
    {
        return "Hello World";
    }

    [HttpPost]
    public async Task<IActionResult> GetCompletion(
        [FromBody] CompletionRequest request,
        CancellationToken cancellationToken
    )
    {
        if (request.Context?.Count > 0)
        {
            var embeddings = await _llmService.GenerateEmbeddingsAsync(
                request.Prompt,
                cancellationToken
            );
            var similarContexts = await _vectorStore.SearchSimilarAsync(
                embeddings,
                limit: 5,
                cancellationToken: cancellationToken
            );

            var contextPrompt = string.Join(
                "\n\n",
                similarContexts.Select(x => x.Metadata["text"])
            );
            request.Prompt = $"Context:\n{contextPrompt}\n\nQuestion: {request.Prompt}\nAnswer:";
        }

        var response = await _llmService.GetCompletionAsync(request, cancellationToken);
        return Ok(new { response });
    }
}
