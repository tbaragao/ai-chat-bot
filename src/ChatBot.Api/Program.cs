using ChatBot.Api;
using ChatBot.Api.Services;
using Microsoft.Extensions.AI;
using Pinecone;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var openAiApiKey = builder.Configuration.GetSection("OpenAiApiKey").Value;
var pineconeAiApiKey = builder.Configuration.GetSection("PineconeApiKey").Value;

builder.Services.AddSingleton<StringEmbeddingGenerator>(s => new OpenAI.Embeddings.EmbeddingClient(
    "text-embedding-3-small",
    openAiApiKey
).AsIEmbeddingGenerator());

builder.Services.AddSingleton<IndexClient>(p => new PineconeClient(pineconeAiApiKey).Index("dt-ai-chatbot"));

builder.Services.AddSingleton<WikipediaService>();
builder.Services.AddSingleton<IndexBuilder>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var indexer = app.Services.GetRequiredService<IndexBuilder>();

await indexer.BuildDocumentIndex(SourceData.LandmarkNames);