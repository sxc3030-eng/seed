namespace Seed.Engine.Storage;

public sealed class ProjectMetadata
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Goal { get; init; } = string.Empty;
    public DateTime ModifiedUtc { get; init; }
}
