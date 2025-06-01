using AiAssistant.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AiAssistant.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VectorController : ControllerBase
{
    private readonly ILLMService _llmService;
    private readonly IVectorStore _vectorStore;

    public VectorController(ILLMService llmService, IVectorStore vectorStore)
    {
        _llmService = llmService;
        _vectorStore = vectorStore;
    }

    [HttpPost("store")]
    public async Task<IActionResult> StoreVector([FromBody] StoreVectorRequest request, CancellationToken cancellationToken)
    {
        var embeddings = await _llmService.GenerateEmbeddingsAsync(request.Text, cancellationToken);
        
        await _vectorStore.StoreVectorAsync(
            request.Id ?? Guid.NewGuid().ToString(),
            embeddings,
            new Dictionary<string, string> { { "text", request.Text } },
            cancellationToken);

        return Ok(new { message = "Vector stored successfully" });
    }

    [HttpPost("search")]
    public async Task<IActionResult> SearchSimilar([FromBody] SearchRequest request, CancellationToken cancellationToken)
    {
        var embeddings = await _llmService.GenerateEmbeddingsAsync(request.Query, cancellationToken);
        var results = await _vectorStore.SearchSimilarAsync(
            embeddings,
            request.Limit ?? 5,
            request.ScoreThreshold ?? 0.7f,
            cancellationToken);

        return Ok(results);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteVector(string id, CancellationToken cancellationToken)
    {
        await _vectorStore.DeleteVectorAsync(id, cancellationToken);
        return Ok(new { message = "Vector deleted successfully" });
    }
}

public class StoreVectorRequest
{
    public string? Id { get; set; }
    public required string Text { get; set; }
}

public class SearchRequest
{
    public required string Query { get; set; }
    public int? Limit { get; set; }
    public float? ScoreThreshold { get; set; }
} 