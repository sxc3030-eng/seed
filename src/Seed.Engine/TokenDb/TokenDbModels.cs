namespace Seed.Engine.TokenDb;

public sealed class GrammarProfile
{
    public string ProjectType { get; init; } = string.Empty;
    public string Version { get; init; } = "1.0";
    public List<VerbDefinition> Verbs { get; init; } = new();
}

public sealed class VerbDefinition
{
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Color { get; init; } = string.Empty;
    public List<string> AcceptsModifiers { get; init; } = new();
}
