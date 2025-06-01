using System.Collections.Generic;
using AiAssistant.Core.Interfaces;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace AiAssistant.Core.Services;

public class QdrantVectorStore : IVectorStore
{
    private readonly QdrantClient _client;
    private readonly string _collectionName;

    public QdrantVectorStore(string host, int grpcPort, string collectionName)
    {
        _client = new QdrantClient(host, grpcPort);
        _collectionName = collectionName;
    }

    public async Task StoreVectorAsync(
        string id,
        float[] vector,
        Dictionary<string, string> metadata,
        CancellationToken cancellationToken = default
    )
    {
        var point = new PointStruct { Id = new PointId { Uuid = id } };

        point.Vectors = new Vectors { Vector = new Vector() { Data = { vector } } };

        foreach (var kvp in metadata)
        {
            point.Payload[kvp.Key] = new Value { StringValue = kvp.Value };
        }

        await _client.UpsertAsync(
            _collectionName,
            new List<PointStruct> { point },
            cancellationToken: cancellationToken
        );
    }

    public async Task<List<(float Score, Dictionary<string, string> Metadata)>> SearchSimilarAsync(
        float[] queryVector,
        int limit = 5,
        float scoreThreshold = 0.7f,
        CancellationToken cancellationToken = default
    )
    {
        var searchResult = await _client.SearchAsync(
            _collectionName,
            queryVector,
            limit: (uint)limit,
            cancellationToken: cancellationToken
        );

        return searchResult
            .Where(x => x.Score >= scoreThreshold)
            .Select(x => (x.Score, x.Payload.ToDictionary(p => p.Key, p => p.Value.StringValue)))
            .ToList();
    }

    public async Task DeleteVectorAsync(string id, CancellationToken cancellationToken = default)
    {
        await _client.DeleteAsync(
            _collectionName,
            ulong.Parse(id),
            cancellationToken: cancellationToken
        );
    }

    public async Task<bool> CollectionExistsAsync(
        string collectionName,
        CancellationToken cancellationToken = default
    )
    {
        var collections = await _client.ListCollectionsAsync(cancellationToken);
        return collections.Contains(collectionName);
    }

    public async Task CreateCollectionAsync(
        string collectionName,
        int vectorSize,
        CancellationToken cancellationToken = default
    )
    {
        var vectorParams = new VectorParams
        {
            Size = (ulong)vectorSize,
            Distance = Distance.Cosine,
        };

        await _client.CreateCollectionAsync(
            collectionName,
            vectorParams,
            cancellationToken: cancellationToken
        );
    }
}
