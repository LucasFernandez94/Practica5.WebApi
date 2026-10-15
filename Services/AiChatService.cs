using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Practica5.WebApi.Models;

namespace Practica5.WebApi.Services;

// Mismo chat de la P1, pero con la abstracción IChatClient: sin JsonObject manual,
// sin DeepClone, sin parseo de /api/chat. El precio: los errores vienen envueltos.
public sealed class AiChatService(IChatClient chat, IOptions<OllamaOptions> options)
{
    private readonly OllamaOptions _opt = options.Value;

    public async Task<ChatResponseDto> ChatearAsync(ChatRequestDto req, CancellationToken ct = default)
    {
        var modelo = string.IsNullOrWhiteSpace(req.Model) ? _opt.ChatModel : req.Model.Trim();
        var messages = req.Messages
            .Select(m => new ChatMessage(ToRole(m.Role), m.Content))
            .ToList();

        if (messages.Count == 0 || messages[0].Role != ChatRole.System)
            messages.Insert(0, new ChatMessage(ChatRole.System,
                "Sos un asistente breve y claro. Respondé en español rioplatense."));

        var inicio = DateTime.UtcNow;
        ChatResponse response;
        try
        {
            response = await chat.GetResponseAsync(messages, new ChatOptions { ModelId = modelo }, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException($"Falló el chat vía IChatClient ({ex.GetType().Name}): {ex.Message}", ex);
        }

        return new ChatResponseDto(response.Text.Trim(), modelo, (DateTime.UtcNow - inicio).TotalSeconds);
    }

    private static ChatRole ToRole(string role) => role.Trim().ToLower() switch
    {
        "system" => ChatRole.System,
        "assistant" => ChatRole.Assistant,
        _ => ChatRole.User,
    };
}
