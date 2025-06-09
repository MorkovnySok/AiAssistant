using AiAssistant.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AiAssistant.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VectorController(ILLMService llmService, IVectorStore vectorStore, IChunker chunker)
    : ControllerBase
{
    [HttpPost("store")]
    public async Task<IActionResult> StoreVector(
        [FromBody] StoreVectorRequest request,
        CancellationToken cancellationToken
    )
    {
        var chunks = chunker.ChunkText(request.Text);
        foreach (var (chunk, index) in chunks.Select((c, i) => (c, i)))
        {
            var embeddings = await llmService.GenerateEmbeddingsAsync(chunk, cancellationToken);
            await vectorStore.StoreVectorAsync(
                request.Id ?? Guid.NewGuid().ToString(), // or use a hash
                embeddings,
                new Dictionary<string, string>
                {
                    { "text", chunk },
                    { "parentId", request.Id ?? "" },
                    { "chunkIndex", index.ToString() },
                },
                cancellationToken
            );
        }

        return Ok();
    }

    [HttpPost("search")]
    public async Task<IActionResult> SearchSimilar(
        [FromBody] SearchRequest request,
        CancellationToken cancellationToken
    )
    {
        var embeddings = await llmService.GenerateEmbeddingsAsync(request.Query, cancellationToken);
        var results = await vectorStore.SearchSimilarAsync(
            embeddings,
            request.Limit ?? 5,
            request.ScoreThreshold ?? 0.7f,
            cancellationToken
        );

        return Ok(results);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteVector(string id, CancellationToken cancellationToken)
    {
        await vectorStore.DeleteVectorAsync(id, cancellationToken);
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
