namespace AiAssistant.Core.Models;

public class LLMConfig
{
    public string ModelName { get; set; } = string.Empty;
    public string EndpointUrl { get; set; } = "http://localhost:11434";
    public Dictionary<string, string> Parameters { get; set; } = new();
} 