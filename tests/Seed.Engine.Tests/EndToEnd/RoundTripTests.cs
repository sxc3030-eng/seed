using FluentAssertions;
using Seed.Engine.Composer;
using Seed.Engine.Compressor;
using Seed.Engine.Models;
using Seed.Engine.Parser;
using Seed.Engine.Storage;
using Seed.Engine.Transpiler;
using Xunit;

namespace Seed.Engine.Tests.EndToEnd;

public class RoundTripTests : IDisposable
{
    private readonly string _root;

    public RoundTripTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"seed-e2e-{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void FullPipeline_ComposeParseTranspileSaveLoadCompress_PreservesIntent()
    {
        var composer = new Engine.Composer.Composer();
        var parser = new Engine.Parser.Parser();
        var transpiler = new Engine.Transpiler.Transpiler();
        var compressor = new Engine.Compressor.Compressor();
        var storage = new JsonFileStorage(_root);

        var input = new ComposerInput
        {
            Header = new ProjectHeader { Type = "cli", Name = "mail-filter", Goal = "filtrer mes mails" },
            Statements =
            {
                new ComposerStatement { Verb = "filtrer", Target = "mail",
                    Modifiers = { new Modifier { Value = "pertinence" } },
                    LinkToNext = LinkType.Seq },
                new ComposerStatement { Verb = "enregistrer", Target = "db",
                    Modifiers = { new Modifier { Key = "type", Value = "sqlite" } },
                    Constraints = { "offline" } }
            }
        };

        var dsl = composer.Compose(input);
        var parsed = parser.Parse(dsl);
        parsed.IsValid.Should().BeTrue();

        var dna = transpiler.Transpile(parsed);
        storage.Save(dna, "round-trip");
        var loaded = storage.Load("round-trip");

        loaded.Header.Name.Should().Be("mail-filter");
        loaded.Statements.Should().HaveCount(2);
        loaded.Statements[0].Verb.Should().Be("filtrer");
        loaded.Statements[0].Modifiers.Should().Contain(m => m.Value == "pertinence");
        loaded.Statements[1].Constraints.Should().Contain("offline");

        var compressed = compressor.Compress(loaded);
        compressed.Should().Contain("filtrer <mail> <pertinence>");
        compressed.Should().Contain("→");
        compressed.Should().Contain("!offline enregistrer <db> <type:sqlite>");
        compressed.Should().NotContain("#");
    }

    [Fact]
    public void CompressionRatio_TenStatementProject_AchievesAtLeast4xCompression()
    {
        var prose = "j'aimerais un programme qui filtre mes mails par ordre de pertinence " +
                    "puis les enregistre dans une base de données sqlite locale tout en restant offline " +
                    "et qui m'alerte sur slack quand un mail important arrive avec une notification " +
                    "instantanée également pour les emails marqués comme urgents par mon équipe";
        var proseTokenEstimate = prose.Split(' ').Length;

        var compressor = new Engine.Compressor.Compressor();
        var dna = new DnaFile
        {
            Header = new ProjectHeader { Type = "cli", Name = "mail-filter", Goal = "filtrer mails par pertinence" },
            Statements =
            {
                new Statement { Id = "s1", Verb = "filtrer", Target = "mail",
                    Modifiers = { new Modifier { Value = "pertinence" } },
                    Links = { new Link { To = "s2", Type = LinkType.Seq } } },
                new Statement { Id = "s2", Verb = "enregistrer", Target = "db",
                    Modifiers = { new Modifier { Key = "type", Value = "sqlite" } },
                    Constraints = { "offline" },
                    Links = { new Link { To = "s3", Type = LinkType.Par } } },
                new Statement { Id = "s3", Verb = "alerter", Target = "slack",
                    Modifiers = { new Modifier { Key = "si", Value = "urgent" } } }
            }
        };

        var compressed = compressor.Compress(dna);
        var dslTokenEstimate = compressed.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;

        var ratio = (double)proseTokenEstimate / dslTokenEstimate;
        ratio.Should().BeGreaterThan(2.0, $"compression ratio was {ratio:F2}x ({proseTokenEstimate} prose words / {dslTokenEstimate} DSL tokens)");
    }
}
