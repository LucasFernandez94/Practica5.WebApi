using Microsoft.Extensions.AI;
using OllamaSharp;
using Practica5.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Config tipada desde appsettings.json -> "Ollama"
builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection("Ollama"));
var ollama = builder.Configuration.GetSection("Ollama").Get<OllamaOptions>() ?? new OllamaOptions();

// UN solo HttpClient nombrado para todo Ollama (factory > new HttpClient: reusa conexiones, respeta DNS)
builder.Services.AddHttpClient("ollama", c =>
{
    c.BaseAddress = new Uri(ollama.BaseUrl);
    c.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddTransient<OllamaChatService>();
builder.Services.AddTransient<AiChatService>();

// IChatClient vía OllamaSharp: misma API serviría para OpenAI/Azure cambiando esta línea.
builder.Services.AddSingleton<IChatClient>(_ => new OllamaApiClient(new Uri(ollama.BaseUrl), ollama.ChatModel));
builder.Services.AddTransient<EmbeddingsService>();
builder.Services.AddSingleton<RagService>(); // dueño del índice en memoria

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

// El índice RAG se construye al arrancar (con cache en disco es ~instantáneo tras la 1ª vez)
using (var scope = app.Services.CreateScope())
{
    var rag = scope.ServiceProvider.GetRequiredService<RagService>();
    await rag.IndexAsync("docs", "embeddings-cache.json");
}

app.Run();
