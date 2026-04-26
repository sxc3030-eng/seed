using Seed.Engine.Models;

namespace Seed.Engine.Composer;

public sealed class ComposerInput
{
    public ProjectHeader Header { get; init; } = new();
    public List<ComposerStatement> Statements { get; init; } = new();
}

public sealed class ComposerStatement
{
    public string Verb { get; init; } = string.Empty;
    public string Target { get; init; } = string.Empty;
    public List<Modifier> Modifiers { get; init; } = new();
    public List<string> Constraints { get; init; } = new();
    public List<string> EntityRefs { get; init; } = new();
    public string? Comment { get; init; }
    public LinkType? LinkToNext { get; init; }
}
