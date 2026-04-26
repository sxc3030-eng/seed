using System.Text.Json;
using Seed.Engine.Models;

namespace Seed.Engine.Storage;

public sealed class JsonFileStorage : IStorage
{
    private readonly string _rootDir;
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public JsonFileStorage(string rootDir)
    {
        _rootDir = rootDir;
        Directory.CreateDirectory(_rootDir);
    }

    public string Save(DnaFile dna, string id)
    {
        var safeId = SanitizeId(id);
        var path = PathFor(safeId);
        File.WriteAllText(path, JsonSerializer.Serialize(dna, Options));
        return safeId;
    }

    public DnaFile Load(string id)
    {
        var path = PathFor(SanitizeId(id));
        if (!File.Exists(path)) throw new FileNotFoundException($"Project not found: {id}", path);
        return JsonSerializer.Deserialize<DnaFile>(File.ReadAllText(path), Options)
            ?? throw new InvalidOperationException($"Failed to deserialize project: {id}");
    }

    public IReadOnlyList<ProjectMetadata> List()
    {
        if (!Directory.Exists(_rootDir)) return Array.Empty<ProjectMetadata>();

        return Directory.EnumerateFiles(_rootDir, "*.dna")
            .Select(file =>
            {
                try
                {
                    var dna = JsonSerializer.Deserialize<DnaFile>(File.ReadAllText(file), Options);
                    if (dna is null) return null;
                    return new ProjectMetadata
                    {
                        Id = Path.GetFileNameWithoutExtension(file),
                        Name = dna.Header.Name,
                        Type = dna.Header.Type,
                        Goal = dna.Header.Goal,
                        ModifiedUtc = File.GetLastWriteTimeUtc(file)
                    };
                }
                catch { return null; }
            })
            .Where(m => m is not null)
            .Select(m => m!)
            .OrderByDescending(m => m.ModifiedUtc)
            .ToList();
    }

    public void Delete(string id)
    {
        var path = PathFor(SanitizeId(id));
        if (File.Exists(path)) File.Delete(path);
    }

    public bool Exists(string id) => File.Exists(PathFor(SanitizeId(id)));

    private string PathFor(string id) => Path.Combine(_rootDir, $"{id}.dna");

    private static string SanitizeId(string id)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = string.Concat(id.Select(c => invalid.Contains(c) ? '_' : c));
        if (string.IsNullOrWhiteSpace(safe)) throw new ArgumentException("Project id cannot be empty", nameof(id));
        return safe;
    }
}
