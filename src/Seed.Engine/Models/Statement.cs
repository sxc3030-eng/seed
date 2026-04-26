namespace Seed.Engine.Models;

public sealed class Statement
{
    public string Id { get; init; } = string.Empty;
    public string Verb { get; init; } = string.Empty;
    public string Target { get; init; } = string.Empty;
    public List<Modifier> Modifiers { get; init; } = new();
    public List<string> Constraints { get; init; } = new();
    public string? Comment { get; init; }
    public List<Link> Links { get; init; } = new();
}
