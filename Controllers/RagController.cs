using Microsoft.AspNetCore.Mvc;
using Practica5.WebApi.Models;
using Practica5.WebApi.Services;

namespace Practica5.WebApi.Controllers;

[ApiController]
[Route("api/rag")]
public sealed class RagController(RagService rag) : ControllerBase
{
    [HttpPost("preguntar")]
    public async Task<ActionResult<RagAskResponseDto>> Preguntar([FromBody] RagAskRequestDto req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Pregunta))
            return BadRequest("Pregunta no puede estar vacía.");
        return Ok(await rag.PreguntarAsync(req, ct));
    }

    [HttpGet("info")]
    public ActionResult<object> Info() => Ok(new { chunks = rag.Chunks });
}
