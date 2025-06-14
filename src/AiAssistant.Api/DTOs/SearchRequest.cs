namespace AiAssistant.Api.Controllers;

public class SearchRequest
{
    public required string Query { get; set; }
    public int? Limit { get; set; }
    public float? ScoreThreshold { get; set; }
}