namespace AiAssistant.Api.Controllers;

public class CrawlUrlRequest
{
    public required string Url { get; set; }
    public required string ContentXPath { get; set; }
    public string? AuthToken { get; set; }
}
