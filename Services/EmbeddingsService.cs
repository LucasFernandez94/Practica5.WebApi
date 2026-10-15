using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Practica5.WebApi.Services;

// Traductor texto -> números (igual que la P4, pero el HttpClient viene de la factory).
public sealed class EmbeddingsService(IHttpClientFactory httpFactory, IOptions<OllamaOptions> options)
{
    private readonly OllamaOptions _opt = options.Value;

    public async Task<List<float[]>> EmbedAsync(IEnumerable<string> textos, CancellationToken ct = default)
    {
        var lista = textos.ToList();
        var http = httpFactory.CreateClient("ollama");
        var response = await http.PostAsJsonAsync("/api/embed",
            new { model = _opt.EmbedModel, input = lista }, ct);
        response.EnsureSuccessStatusCode();

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: ct);
        return doc!.RootElement.GetProperty("embeddings").EnumerateArray()
            .Select(e => e.EnumerateArray().Select(n => n.GetSingle()).ToArray())
            .ToList();
    }
}
