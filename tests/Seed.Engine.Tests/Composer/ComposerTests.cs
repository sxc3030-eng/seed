using FluentAssertions;
using Seed.Engine.Composer;
using Seed.Engine.Models;
using Xunit;

namespace Seed.Engine.Tests.Composer;

public class ComposerTests
{
    private readonly IComposer _composer = new Engine.Composer.Composer();

    [Fact]
    public void Compose_HeaderOnly_EmitsThreeHeaderLines()
    {
        var input = new ComposerInput
        {
            Header = new ProjectHeader { Type = "cli", Name = "x", Goal = "y" },
            Statements = new List<ComposerStatement>()
        };

        var dsl = _composer.Compose(input);

        dsl.Should().Contain("TYPE: cli").And.Contain("NAME: x").And.Contain("GOAL: y");
    }

    [Fact]
    public void Compose_SimpleStatement_FormatsCorrectly()
    {
        var input = new ComposerInput
        {
            Header = new ProjectHeader { Type = "cli", Name = "x", Goal = "y" },
            Statements = new List<ComposerStatement>
            {
                new ComposerStatement { Verb = "filtrer", Target = "mail" }
            }
        };

        var dsl = _composer.Compose(input);

        dsl.Should().Contain("filtrer <mail>");
    }

    [Fact]
    public void Compose_StatementWithModifiers_EmitsAllSlots()
    {
        var input = new ComposerInput
        {
            Header = new ProjectHeader { Type = "cli", Name = "x", Goal = "y" },
            Statements = new List<ComposerStatement>
            {
                new ComposerStatement
                {
                    Verb = "filtrer",
                    Target = "mail",
                    Modifiers = new List<Modifier>
                    {
                        new Modifier { Value = "pertinence" },
                        new Modifier { Key = "format", Value = "json" }
                    }
                }
            }
        };

        var dsl = _composer.Compose(input);

        dsl.Should().Contain("filtrer <mail> <pertinence> <format:json>");
    }

    [Fact]
    public void Compose_TwoStatementsWithSeqLink_UsesArrow()
    {
        var input = new ComposerInput
        {
            Header = new ProjectHeader { Type = "cli", Name = "x", Goal = "y" },
            Statements = new List<ComposerStatement>
            {
                new ComposerStatement { Verb = "filtrer", Target = "mail", LinkToNext = LinkType.Seq },
                new ComposerStatement { Verb = "enregistrer", Target = "db" }
            }
        };

        var dsl = _composer.Compose(input);

        dsl.Should().Contain("filtrer <mail> → enregistrer <db>");
    }

    [Fact]
    public void Compose_Constraint_PrependedWithBang()
    {
        var input = new ComposerInput
        {
            Header = new ProjectHeader { Type = "cli", Name = "x", Goal = "y" },
            Statements = new List<ComposerStatement>
            {
                new ComposerStatement { Verb = "enregistrer", Target = "db", Constraints = new List<string> { "offline" } }
            }
        };

        var dsl = _composer.Compose(input);

        dsl.Should().Contain("!offline enregistrer <db>");
    }

    [Fact]
    public void Compose_Comment_AppendedToLine()
    {
        var input = new ComposerInput
        {
            Header = new ProjectHeader { Type = "cli", Name = "x", Goal = "y" },
            Statements = new List<ComposerStatement>
            {
                new ComposerStatement { Verb = "filtrer", Target = "mail", Comment = "# filtre principal" }
            }
        };

        var dsl = _composer.Compose(input);

        dsl.Should().Contain("filtrer <mail>     # filtre principal");
    }
}
