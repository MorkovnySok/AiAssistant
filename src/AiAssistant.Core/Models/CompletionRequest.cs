namespace AiAssistant.Core.Models;

public class CompletionRequest
{
    public string Prompt { get; set; } = string.Empty;
    public bool UseMemory { get; set; }
}
