using AiAssistant.Api.Startup;
using AiAssistant.Core.Interfaces;
using AiAssistant.Core.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddLogging(x => x.AddConsole());

var provider = builder.Configuration["Provider"];
switch (provider?.ToLowerInvariant())
{
    case "openai":
        builder.Services.AddSingleton<ILLMService, OpenAiService>();
        break;
    case "local" or "ollama":
    default:
        var ollamaService = await new OllamaBuilder(builder.Configuration).SetupOllamaService();
        builder.Services.AddSingleton<ILLMService>(ollamaService);
        break;
}

// Configure Qdrant
builder.Services.AddSingleton<IVectorStore>(sp => new QdrantVectorStore(
    builder.Configuration["Qdrant:Host"] ?? "qdrant",
    int.Parse(builder.Configuration["Qdrant:GrpcPort"] ?? "6334"),
    builder.Configuration["Qdrant:DefaultCollection"] ?? "ai_assistant"
));
builder.Services.AddTransient<IChunker, Chunker>();

var corsOrigins = builder.Configuration.GetSection("CorsOrigins").Get<string[]>() ?? new string[0];

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowConfiguredOrigins",
        policy => policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()
    );
});

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

app.UseCors("AllowConfiguredOrigins");

app.Run();
