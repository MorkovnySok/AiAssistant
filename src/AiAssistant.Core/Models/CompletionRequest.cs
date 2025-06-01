namespace AiAssistant.Core.Models;

public class CompletionRequest
{
    public string Prompt { get; set; } = string.Empty;
    public List<string>? Context { get; set; }
    public LLMConfig? ModelConfig { get; set; }
} 