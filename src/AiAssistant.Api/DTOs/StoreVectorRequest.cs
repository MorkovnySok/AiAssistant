namespace AiAssistant.Api.Controllers;

public class StoreVectorRequest
{
    public string? Id { get; set; }
    public required string Text { get; set; }
}
