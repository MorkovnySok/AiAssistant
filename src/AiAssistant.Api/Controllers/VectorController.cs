using System.Diagnostics;
using System.Threading.Channels;
using AiAssistant.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using SimpleCrawler;

namespace AiAssistant.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VectorController(
    ILLMService llmService,
    IVectorStore vectorStore,
    IChunker chunker,
    IConfiguration config,
    ILogger<VectorController> logger
) : ControllerBase
{
    [HttpPost("store")]
    public async Task<IActionResult> StoreVector(
        [FromBody] StoreVectorRequest request,
        CancellationToken cancellationToken
    )
    {
        var chunks = chunker.ChunkText(request.Text).ToList();
        var chunkId = request.Id ?? Guid.NewGuid().ToString();

        for (var index = 0; index < chunks.Count; index++)
        {
            var chunk = chunks[index];
            if (index % 10 == 0)
            {
                logger.LogInformation($"Storing {chunks.Count} chunk {index}/{chunks.Count}");
            }

            var embeddings = await llmService.GenerateEmbeddingsAsync(chunk, cancellationToken);
            await vectorStore.StoreVectorAsync(
                chunkId,
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

        return Ok(new { Chunks = chunks.Count });
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadTextFile(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        using var reader = new StreamReader(file.OpenReadStream());
        var fileContent = await reader.ReadToEndAsync();
        return await StoreVector(
            new StoreVectorRequest { Text = fileContent },
            CancellationToken.None
        );
    }

    [HttpPost("crawl")]
    public async Task<IActionResult> CrawlUrl(CrawlUrlRequest request)
    {
        var crawler = new Crawler(request.Url, request.ContentXPath, request.AuthToken);
        var results = await crawler.CrawlAsync();
        logger.LogInformation($"Crawled url: {request.Url} with pages: {results.Count}");
        var sw = new Stopwatch();
        var i = 1;
        foreach (var result in results)
        {
            sw.Restart();
            await StoreVector(
                new StoreVectorRequest() { Text = result.Content },
                CancellationToken.None
            );
            sw.Stop();
            logger.LogInformation($"Stored url: {result.Url} took {sw.ElapsedMilliseconds}ms");
            i++;
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

    [HttpDelete("clear")]
    public async Task<IActionResult> Clear()
    {
        var collectionName = config["Qdrant:DefaultCollection"] ?? "ai_assistant";
        await vectorStore.DeleteCollectionAsync(collectionName);
        await vectorStore.CreateCollectionAsync(collectionName, 4096);
        return Ok();
    }
}
