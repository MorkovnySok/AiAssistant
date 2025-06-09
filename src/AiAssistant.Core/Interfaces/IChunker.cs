namespace AiAssistant.Core.Interfaces;

public interface IChunker
{
    IEnumerable<string> ChunkText(string text, int maxWords = 256, int overlap = 90);
}
