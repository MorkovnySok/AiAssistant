using AiAssistant.Core.Interfaces;

namespace AiAssistant.Core.Services;

public class Chunker : IChunker
{
    public IEnumerable<string> ChunkText(string text, int maxWords = 256, int overlap = 90)
    {
        var words = text.Split(' ');
        for (int i = 0; i < words.Length; i += maxWords - overlap)
        {
            yield return string.Join(" ", words.Skip(i).Take(maxWords));
            if (i + maxWords >= words.Length)
                break;
        }
    }
}
