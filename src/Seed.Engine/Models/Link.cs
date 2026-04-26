using System.Text.Json.Serialization;

namespace Seed.Engine.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LinkType
{
    Seq,
    Par,
    Alt
}

public sealed class Link
{
    public string To { get; init; } = string.Empty;
    public LinkType Type { get; init; } = LinkType.Seq;
}
