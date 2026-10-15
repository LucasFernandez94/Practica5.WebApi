using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Practica5.WebApi.Models;

namespace Practica5.WebApi.Services;

// Práctica 1 llevada a API: el historial viene en el request, el HttpClient sale de la factory.
public sealed class OllamaChatService(IHttpClientFactory httpFactory, IOptions<OllamaOptions> options)
{
    private readonly OllamaOptions _opt = options.Value;

    public async Task<ChatResponseDto> ChatearAsync(ChatRequestDto req, CancellationToken ct = default)
    {
        var modelo = string.IsNullOrWhiteSpace(req.Model) ? _opt.ChatModel : req.Model.Trim();
        var messages = new JsonArray(
            req.Messages.Select(m => new JsonObject { ["role"] = m.Role, ["content"] = m.Content }).ToArray());

        // Si el caller no mandó system prompt, agregamos el default de la P1.
        if (req.Messages.Count == 0 || !req.Messages[0].Role.Equals("system", StringComparison.OrdinalIgnoreCase))
            messages.Insert(0, new JsonObject
            {
                ["role"] = "system",
                ["content"] = "Sos un asistente breve y claro. Respondé en español rioplatense."
            });

        var body = new JsonObject
        {
            ["model"] = modelo,
            ["messages"] = messages,
            ["stream"] = false
        };

        var inicio = DateTime.UtcNow;
        var http = httpFactory.CreateClient("ollama");
        HttpResponseMessage response;
        try
        {
            response = await http.PostAsJsonAsync("/api/chat", body, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"No pude conectar con Ollama en {_opt.BaseUrl}. ¿Está corriendo?", ex);
        }
        response.EnsureSuccessStatusCode();

        using var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: ct);
        var texto = doc!.RootElement.GetProperty("message").GetProperty("content").GetString()?.Trim() ?? "(vacío)";
        return new ChatResponseDto(texto, modelo, (DateTime.UtcNow - inicio).TotalSeconds);
    }
}
