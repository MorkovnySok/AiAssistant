namespace AiAssistant.Core.Interfaces;

public interface IVectorStore
{
    Task StoreVectorAsync(
        string id,
        float[] vector,
        Dictionary<string, string> metadata,
        CancellationToken cancellationToken = default
    );
    Task<List<(float Score, Dictionary<string, string> Metadata)>> SearchSimilarAsync(
        float[] queryVector,
        int limit = 5,
        float scoreThreshold = 0.7f,
        CancellationToken cancellationToken = default
    );
    Task DeleteVectorAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> CollectionExistsAsync(
        string collectionName,
        CancellationToken cancellationToken = default
    );
    Task CreateCollectionAsync(
        string collectionName,
        int vectorSize,
        CancellationToken cancellationToken = default
    );
    Task DeleteCollectionAsync();
}
