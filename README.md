# Práctica 5 — Web API (.NET 8 + Ollama local)

La P1 (chat) y la P4 (RAG) como servicios HTTP, con controllers finitos y services inyectados. Más la comparación crudo vs librería.

```bash
dotnet run
# Swagger: http://localhost:5083/swagger
```

## Endpoints

| Método | Ruta | Qué es |
|---|---|---|
| POST | `/api/chat` | Chat con protocolo Ollama crudo (`HttpClient` + JSON a mano, como la P1) |
| POST | `/api/chat/ai` | Mismo chat vía `Microsoft.Extensions.AI` + `OllamaSharp` (lado a lado para comparar) |
| POST | `/api/rag/preguntar` | RAG sobre `docs/` (NubeSur): responde con citas o dice que no sabe |
| GET | `/api/rag/info` | Cantidad de chunks indexados |

```bash
curl -X POST http://localhost:5083/api/chat/ai \
  -H "Content-Type: application/json" \
  -d '{"messages":[{"role":"user","content":"Hola, ¿quién sos?"}]}'

curl -X POST http://localhost:5083/api/rag/preguntar \
  -H "Content-Type: application/json" \
  -d '{"pregunta": "¿Cuántos días tengo para devolver un producto?"}'
```

## Arquitectura (lo nuevo vs las consolas)

- `new HttpClient` → `AddHttpClient("ollama")` + `IHttpClientFactory`.
- Modelo/URL hardcodeados → `appsettings.json` (`Ollama:BaseUrl/ChatModel/EmbedModel`) + `IOptions<OllamaOptions>`.
- `Controllers/` finitos (validan y delegan), lógica en `Services/`.
- `RagService` singleton indexa `docs/` al arrancar (con cache en disco es instantáneo tras la 1ª vez).
- `docs/*.md` viajan junto al `.exe` (`CopyToOutputDirectory`).

## Archivos

- `Controllers/ChatController.cs`, `Controllers/RagController.cs`
- `Services/OllamaChatService.cs` (chat crudo), `Services/AiChatService.cs` (`IChatClient`),
  `Services/RagService.cs`, `Services/EmbeddingsService.cs`, `Services/VectorStore.cs`
- `Models/ApiModels.cs` (DTOs compartidos por ambas versiones del chat)
