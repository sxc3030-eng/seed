using FluentAssertions;
using Seed.Engine.Compressor;
using Seed.Engine.Models;
using Xunit;

namespace Seed.Engine.Tests.Compressor;

public class CompressorTests
{
    private readonly ICompressor _compressor = new Engine.Compressor.Compressor();

    [Fact]
    public void Compress_StripsAllComments()
    {
        var dna = new DnaFile
        {
            Header = new ProjectHeader { Type = "cli", Name = "x", Goal = "y" },
            Statements =
            {
                new Statement
                {
                    Id = "s1",
                    Verb = "filtrer",
                    Target = "mail",
                    Comment = "# this should be stripped"
                }
            }
        };

        var output = _compressor.Compress(dna);

        output.Should().NotContain("#");
        output.Should().NotContain("this should be stripped");
    }

    [Fact]
    public void Compress_PreservesStatementChainStructure()
    {
        var dna = new DnaFile
        {
            Header = new ProjectHeader { Type = "cli", Name = "x", Goal = "y" },
            Statements =
            {
                new Statement
                {
                    Id = "s1", Verb = "filtrer", Target = "mail",
                    Links = { new Link { To = "s2", Type = LinkType.Seq } }
                },
                new Statement { Id = "s2", Verb = "enregistrer", Target = "db" }
            }
        };

        var output = _compressor.Compress(dna);

        output.Should().Contain("filtrer <mail>");
        output.Should().Contain("→");
        output.Should().Contain("enregistrer <db>");
    }

    [Fact]
    public void Compress_NormalizesWhitespace_NoConsecutiveSpaces()
    {
        var dna = new DnaFile
        {
            Header = new ProjectHeader { Type = "cli", Name = "x", Goal = "y" },
            Statements = { new Statement { Id = "s1", Verb = "filtrer", Target = "mail" } }
        };

        var output = _compressor.Compress(dna);

        output.Should().NotContain("  ");
    }

    [Fact]
    public void Compress_IsIdempotent_SameInputProducesSameOutput()
    {
        var dna = new DnaFile
        {
            Header = new ProjectHeader { Type = "cli", Name = "x", Goal = "y" },
            Statements = { new Statement { Id = "s1", Verb = "filtrer", Target = "mail" } }
        };

        var first = _compressor.Compress(dna);
        var second = _compressor.Compress(dna);

        first.Should().Be(second);
    }

    [Fact]
    public void Compress_OmitsEmptyHeaderFields()
    {
        var dna = new DnaFile
        {
            Header = new ProjectHeader { Type = "cli", Name = string.Empty, Goal = "y" },
            Statements = { new Statement { Id = "s1", Verb = "filtrer", Target = "mail" } }
        };

        var output = _compressor.Compress(dna);

        output.Should().NotContain("NAME:");
        output.Should().Contain("TYPE: cli");
        output.Should().Contain("GOAL: y");
    }
}
