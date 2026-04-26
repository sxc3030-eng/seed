namespace Seed.Engine.Models;

/// <summary>
/// Represents a modifier on a DSL statement.
/// Key is null for simple modifiers like &lt;pertinence&gt;.
/// Key is set for qualified modifiers like &lt;db:sqlite&gt;.
/// </summary>
public sealed class Modifier
{
    public string? Key { get; init; }
    public string Value { get; init; } = string.Empty;
}
