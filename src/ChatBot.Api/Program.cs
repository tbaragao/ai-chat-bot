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

builder.Services.AddSingleton<DocumentStore>();

builder.Services.AddSingleton<VectorSearchServices>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCors", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


var app = builder.Build();

app.UseCors("FrontendCors");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// var indexer = app.Services.GetRequiredService<IndexBuilder>();
//
// await indexer.BuildDocumentIndex(SourceData.LandmarkNames);

app.MapGet("/search", async (string query, VectorSearchServices searchService) =>
{
    var results = await searchService.Search(query, 3);
    return Results.Ok(results);
});

app.Run();