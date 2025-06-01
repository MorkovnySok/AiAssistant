using AiAssistant.Core.Interfaces;
using AiAssistant.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure HttpClient for Ollama
builder.Services.AddHttpClient<ILLMService, OllamaService>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Ollama:BaseUrl"] ?? "http://localhost:11434"
    );
});

// Configure Qdrant
builder.Services.AddSingleton<IVectorStore>(sp => new QdrantVectorStore(
    builder.Configuration["Qdrant:Host"] ?? "localhost",
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

app.UseHttpsRedirection();
app.UseAuthorization();
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
