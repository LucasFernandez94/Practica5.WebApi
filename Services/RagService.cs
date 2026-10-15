using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Practica5.WebApi.Models;

namespace Practica5.WebApi.Services;

// Práctica 4 llevada a API: singleton que indexa al arrancar y responde con citas.
public sealed class RagService(
    IHttpClientFactory httpFactory,
    EmbeddingsService emb,
    IOptions<OllamaOptions> options,
    ILogger<RagService> logger)
{
    private readonly OllamaOptions _opt = options.Value;
    private readonly VectorStore _store = new();

    public int Chunks => _store.Count;

    public async Task IndexAsync(string docsDir, string cachePath, CancellationToken ct = default)
    {
        logger.LogInformation("Indexando {Docs}...", docsDir);
        await _store.IndexarAsync(docsDir, cachePath, textos => emb.EmbedAsync(textos, ct));
        logger.LogInformation("Índice listo: {Count} chunks.", _store.Count);
    }

    public async Task<RagAskResponseDto> PreguntarAsync(RagAskRequestDto req, CancellationToken ct = default)
    {
        var inicio = DateTime.UtcNow;
        var qv = (await emb.EmbedAsync([req.Pregunta], ct))[0];
        var hits = _store.Buscar(qv, Math.Clamp(req.TopK, 1, 10));

        var contexto = string.Join("\n\n", hits.Select((h, i) => $"[{i + 1}] ({h.Chunk.Doc}) {h.Chunk.Texto}"));
        var body = new JsonObject
        {
            ["model"] = _opt.ChatModel,
            ["messages"] = new JsonArray
            {
                new JsonObject
                {
                    ["role"] = "system",
                    ["content"] = "Respondé en español rioplatense, breve, usando ÚNICAMENTE el contexto de abajo. " +
                                  "Si la respuesta no está en el contexto, decí exactamente: \"No tengo esa información en mis documentos.\" " +
                                  "No inventes datos, plazos ni números."
                },
                new JsonObject { ["role"] = "user", ["content"] = $"Contexto:\n{contexto}\n\nPregunta: {req.Pregunta}" }
            },
            ["stream"] = false
        };

        var http = httpFactory.CreateClient("ollama");
        var response = await http.PostAsJsonAsync("/api/chat", body, ct);
        response.EnsureSuccessStatusCode();

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: ct);
        var texto = doc!.RootElement.GetProperty("message").GetProperty("content").GetString()?.Trim() ?? "(vacío)";

        return new RagAskResponseDto(
            texto,
            hits.Select(h => new CitaDto(h.Chunk.Doc, Math.Round(h.Score, 3), h.Chunk.Texto)).ToList(),
            (DateTime.UtcNow - inicio).TotalSeconds);
    }
}
