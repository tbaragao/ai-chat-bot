using Microsoft.Extensions.AI;
using System.Collections.Immutable;
using Pinecone;

namespace ChatBot.Api.Services;

public class IndexBuilder(StringEmbeddingGenerator embeddingGenerator, IndexClient pineconeClient, WikipediaService wikipediaService, DocumentStore documentStore)
{
    public async Task BuildDocumentIndex(string[] pageTitles)
    {
        foreach (var landmark in pageTitles)
        {
            var wikiPage = await wikipediaService.GetWikipediaPageForTitle(landmark);
            
            var embedding = await embeddingGenerator.GenerateAsync([wikiPage.Content], new EmbeddingGenerationOptions()
            {
                Dimensions = 512,
            });

            var vectorArray = embedding[0].Vector.ToArray();

            var pineconeVector = new Vector
            {
                Id = wikiPage.Id,
                Values = vectorArray,
                Metadata = new Metadata
                {
                    { "title", wikiPage.Title }
                }
            };

            await pineconeClient.UpsertAsync(new UpsertRequest
            {
                Vectors = [pineconeVector]
            });
            
            documentStore.SaveDocument(wikiPage);
        }
    }
}