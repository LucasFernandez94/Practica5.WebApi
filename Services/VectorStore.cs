using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Practica5.WebApi.Services;

// Base vectorial casera (igual que la P4): chunks + coseno + cache en disco. Clase pura, sin HTTP.
public sealed class VectorStore
{
    public sealed record Chunk(string Doc, string Texto, float[] Vector);
    public sealed record Hit(Chunk Chunk, double Score);

    private readonly List<Chunk> _chunks = new();
    public int Count => _chunks.Count;

    public async Task IndexarAsync(string docsDir, string cachePath, Func<IEnumerable<string>, Task<List<float[]>>> embed)
    {
        var pendientes = new List<(string Doc, string Texto, string Hash)>();
        foreach (var file in Directory.GetFiles(docsDir, "*.md").OrderBy(f => f))
        {
            var doc = Path.GetFileNameWithoutExtension(file);
            var parrafos = File.ReadAllText(file)
                .Split(["\n\n", "\r\n\r\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(p => p.Length > 20);
            foreach (var p in parrafos)
                pendientes.Add((doc, p, Hash(p)));
        }

        var cache = CargarCache(cachePath);
        var faltantes = pendientes.Where(p => !cache.ContainsKey(p.Hash)).ToList();

        if (faltantes.Count > 0)
        {
            var vectores = await embed(faltantes.Select(f => f.Texto));
            for (var i = 0; i < faltantes.Count; i++)
                cache[faltantes[i].Hash] = vectores[i];
            GuardarCache(cachePath, cache);
        }

        foreach (var (doc, texto, hash) in pendientes)
            _chunks.Add(new Chunk(doc, texto, cache[hash]));
    }

    public List<Hit> Buscar(float[] query, int topK = 3) =>
        _chunks.Select(c => new Hit(c, Coseno(query, c.Vector)))
            .OrderByDescending(h => h.Score)
            .Take(topK)
            .ToList();

    public static double Coseno(float[] a, float[] b)
    {
        double dot = 0, na = 0, nb = 0;
        for (var i = 0; i < a.Length; i++) { dot += a[i] * b[i]; na += a[i] * a[i]; nb += b[i] * b[i]; }
        return na == 0 || nb == 0 ? 0 : dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }

    private static string Hash(string s) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s)));

    private static Dictionary<string, float[]> CargarCache(string path)
    {
        if (!File.Exists(path)) return new();
        try { return JsonSerializer.Deserialize<Dictionary<string, float[]>>(File.ReadAllText(path)) ?? new(); }
        catch (JsonException) { return new(); }
    }

    private static void GuardarCache(string path, Dictionary<string, float[]> cache) =>
        File.WriteAllText(path, JsonSerializer.Serialize(cache));
}
