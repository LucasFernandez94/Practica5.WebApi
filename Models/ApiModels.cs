namespace Practica5.WebApi.Models;

// POST /api/chat
public sealed record ChatMessageDto(string Role, string Content);
public sealed record ChatRequestDto(List<ChatMessageDto> Messages, string? Model = null);
public sealed record ChatResponseDto(string Respuesta, string Modelo, double Segundos);

// POST /api/rag/preguntar
public sealed record RagAskRequestDto(string Pregunta, int TopK = 3);
public sealed record CitaDto(string Doc, double Score, string Texto);
public sealed record RagAskResponseDto(string Respuesta, List<CitaDto> Citas, double Segundos);
