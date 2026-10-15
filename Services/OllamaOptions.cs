namespace Practica5.WebApi.Services;

// Se bindea desde appsettings.json -> "Ollama". Nada hardcodeado en los services.
public sealed class OllamaOptions
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string ChatModel { get; set; } = "qwen2.5:1.5b";
    public string EmbedModel { get; set; } = "nomic-embed-text";
}
