namespace Seed.Engine.Models;

public sealed class DnaFile
{
    public string Version { get; init; } = "1.0";
    public ProjectHeader Header { get; init; } = new();
    public List<Statement> Statements { get; init; } = new();
    public Dictionary<string, RenderingHint>? Rendering { get; init; }
}

public sealed class RenderingHint
{
    public double[] Pos { get; init; } = Array.Empty<double>();
}
