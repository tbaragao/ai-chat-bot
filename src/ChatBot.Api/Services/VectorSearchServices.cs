using ChatBot.Api.Models;
using Microsoft.Extensions.AI;
using Pinecone;

namespace ChatBot.Api.Services;

public class VectorSearchServices(
    StringEmbeddingGenerator embeddingGenerator,
    IndexClient pineconeIndex,
    DocumentStore contextStore
)
{
    public async Task<List<Documents>> Search(string query, int articlesAmount)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var embeddings = await embeddingGenerator.GenerateAsync([query], new EmbeddingGenerationOptions()
        {
            Dimensions = 512
        });

        var vector = embeddings[0].Vector.ToArray();

        var response = await pineconeIndex.QueryAsync(new QueryRequest()
        {
            Vector = vector,
            TopK = (uint)articlesAmount,
            IncludeMetadata = true
        });

        var matches = (response.Matches ?? []).ToList();
        if (matches.Any() is false)
        {
            return [];
        }
        var ids = matches.Select(m => m.Id).Where(id => string.IsNullOrEmpty(id) is false);
        
        var documents = contextStore.GetDocuments(ids);

        var scorebyId = matches.Where(m => m.Id is not null)
            .ToDictionary(m => m.Id, m => m.Score);
        
        var ordered = documents.OrderByDescending(d => scorebyId.GetValueOrDefault(d.Id, 0f))
            .Take(articlesAmount)
            .ToList();
        
        return ordered;
    }   
}