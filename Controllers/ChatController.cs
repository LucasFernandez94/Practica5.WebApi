using Microsoft.AspNetCore.Mvc;
using Practica5.WebApi.Models;
using Practica5.WebApi.Services;

namespace Practica5.WebApi.Controllers;

// Controller finito: valida entrada, delega al service, devuelve DTO. Sin lógica LLM acá.
[ApiController]
[Route("api/chat")]
public sealed class ChatController(OllamaChatService chat) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ChatResponseDto>> Post([FromBody] ChatRequestDto req, CancellationToken ct)
    {
        if (req.Messages.Count == 0)
            return BadRequest("Messages no puede estar vacío.");
        return Ok(await chat.ChatearAsync(req, ct));
    }

    // Misma operación vía Microsoft.Extensions.AI + OllamaSharp: comparar con el crudo.
    [HttpPost("ai")]
    public async Task<ActionResult<ChatResponseDto>> PostAi(
        [FromBody] ChatRequestDto req, [FromServices] AiChatService ai, CancellationToken ct)
    {
        if (req.Messages.Count == 0)
            return BadRequest("Messages no puede estar vacío.");
        return Ok(await ai.ChatearAsync(req, ct));
    }
}
