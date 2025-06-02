using AiAssistant.Core.Interfaces;
using AiAssistant.Core.Services;
using OllamaSharp;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure HttpClient for Ollama
var model = builder.Configuration["Ollama:DefaultModel"] ?? "deepseek-r1:8b";
var ollamaServer = builder.Configuration["Ollama:BaseUrl"] ?? "http://ollama:11434";
Console.WriteLine("Ollama model is " + model);
Console.WriteLine("Ollama is running at " + ollamaServer);
var ollamaClient = new OllamaApiClient(ollamaServer);
var existingModels = await ollamaClient.ListLocalModelsAsync(CancellationToken.None);
if (existingModels.All(x => x.Name != model))
{
    Console.WriteLine("Ollama model is not pulled, starting to pull...");
    var i = 0;
    await foreach (var status in ollamaClient.PullModelAsync(model, CancellationToken.None))
    {
        if (i == 20)
        {
            Console.WriteLine(
                $"{status!.Percent}% {status.Status}. Pulled {status.Completed / 1024 / 1024} mb out of {status.Total / 1024 / 1024} mb"
            );
            i = 0;
        }
        i++;
    }
}

builder.Services.AddTransient<ILLMService, OllamaService>(x => new OllamaService(
    model,
    ollamaServer
));

// Configure Qdrant
builder.Services.AddSingleton<IVectorStore>(sp => new QdrantVectorStore(
    builder.Configuration["Qdrant:Host"] ?? "qdrant",
    int.Parse(builder.Configuration["Qdrant:GrpcPort"] ?? "6334"),
    builder.Configuration["Qdrant:DefaultCollection"] ?? "ai_assistant"
));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();
// app.UseAuthorization();
app.MapControllers();

// Ensure Qdrant collection exists
using (var scope = app.Services.CreateScope())
{
    var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStore>();
    var collectionName = builder.Configuration["Qdrant:DefaultCollection"] ?? "ai_assistant";

    if (!await vectorStore.CollectionExistsAsync(collectionName))
    {
        await vectorStore.CreateCollectionAsync(collectionName, vectorSize: 4096); // Adjust vector size based on your model
    }
}

app.Run();
