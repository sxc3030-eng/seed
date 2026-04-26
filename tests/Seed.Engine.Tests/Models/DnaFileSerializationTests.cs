using System.Text.Json;
using FluentAssertions;
using Seed.Engine.Models;
using Xunit;

namespace Seed.Engine.Tests.Models;

public class DnaFileSerializationTests
{
    [Fact]
    public void DnaFile_RoundTripsThroughJson_PreservingAllFields()
    {
        var original = new DnaFile
        {
            Version = "1.0",
            Header = new ProjectHeader
            {
                Type = "cli",
                Name = "mail-filter",
                Goal = "filtrer mes mails par pertinence"
            },
            Statements = new List<Statement>
            {
                new()
                {
                    Id = "s1",
                    Verb = "filtrer",
                    Target = "mail",
                    Modifiers = new List<Modifier>
                    {
                        new() { Key = null, Value = "pertinence" }
                    },
                    Comment = "filtre principal du flow",
                    Links = new List<Link> { new() { To = "s2", Type = LinkType.Seq } }
                },
                new()
                {
                    Id = "s2",
                    Verb = "enregistrer",
                    Target = "db",
                    Modifiers = new List<Modifier>
                    {
                        new() { Key = "type", Value = "sqlite" }
                    },
                    Constraints = new List<string> { "offline" }
                }
            }
        };

        var json = JsonSerializer.Serialize(original, new JsonSerializerOptions { WriteIndented = true });
        var roundTripped = JsonSerializer.Deserialize<DnaFile>(json);

        roundTripped.Should().BeEquivalentTo(original);
    }
}
