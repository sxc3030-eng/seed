using FluentAssertions;
using Seed.Engine.Models;
using Seed.Engine.Parser;
using Xunit;

namespace Seed.Engine.Tests.Parser;

public class ParserTests
{
    private readonly IParser _parser = new Engine.Parser.Parser();

    [Fact]
    public void Parse_HeaderOnly_PopulatesHeaderFields()
    {
        var dsl = "TYPE: cli\nNAME: mail-filter\nGOAL: filtrer mes mails";
        var result = _parser.Parse(dsl);

        result.IsValid.Should().BeTrue();
        result.Header.Type.Should().Be("cli");
        result.Header.Name.Should().Be("mail-filter");
        result.Header.Goal.Should().Be("filtrer mes mails");
    }

    [Fact]
    public void Parse_SimpleStatement_ProducesOneAstStatement()
    {
        var dsl = "TYPE: cli\nNAME: x\nGOAL: y\nfiltrer <mail>";
        var result = _parser.Parse(dsl);

        result.IsValid.Should().BeTrue();
        result.Statements.Should().HaveCount(1);
        result.Statements[0].Verb.Should().Be("filtrer");
        result.Statements[0].Target.Should().Be("mail");
    }

    [Fact]
    public void Parse_StatementWithModifier_AttachesModifier()
    {
        var dsl = "TYPE: cli\nNAME: x\nGOAL: y\nfiltrer <mail> <pertinence>";
        var result = _parser.Parse(dsl);

        result.Statements[0].Modifiers.Should().HaveCount(1);
        result.Statements[0].Modifiers[0].Value.Should().Be("pertinence");
        result.Statements[0].Modifiers[0].Key.Should().BeNull();
    }

    [Fact]
    public void Parse_QualifiedModifier_SplitsKeyAndValue()
    {
        var dsl = "TYPE: cli\nNAME: x\nGOAL: y\nenregistrer <db> <type:sqlite>";
        var result = _parser.Parse(dsl);

        result.Statements[0].Modifiers.Should().Contain(m => m.Key == "type" && m.Value == "sqlite");
    }

    [Fact]
    public void Parse_SequenceOperator_LinksStatementsAsSeq()
    {
        var dsl = "TYPE: cli\nNAME: x\nGOAL: y\nfiltrer <mail> → enregistrer <db>";
        var result = _parser.Parse(dsl);

        result.Statements.Should().HaveCount(2);
        result.Statements[0].Links.Should().HaveCount(1);
        result.Statements[0].Links[0].Type.Should().Be(LinkType.Seq);
        result.Statements[0].Links[0].To.Should().Be(result.Statements[1].Id);
    }

    [Fact]
    public void Parse_ParallelOperator_LinksStatementsAsPar()
    {
        var dsl = "TYPE: cli\nNAME: x\nGOAL: y\nenregistrer <db> & alerter <slack>";
        var result = _parser.Parse(dsl);

        result.Statements.Should().HaveCount(2);
        result.Statements[0].Links[0].Type.Should().Be(LinkType.Par);
    }

    [Fact]
    public void Parse_AltOperator_LinksStatementsAsAlt()
    {
        var dsl = "TYPE: cli\nNAME: x\nGOAL: y\nnotifier <slack> | retry <3-fois>";
        var result = _parser.Parse(dsl);

        result.Statements.Should().HaveCount(2);
        result.Statements[0].Links[0].Type.Should().Be(LinkType.Alt);
    }

    [Fact]
    public void Parse_Constraint_AttachesToFollowingStatement()
    {
        var dsl = "TYPE: cli\nNAME: x\nGOAL: y\n!offline enregistrer <db>";
        var result = _parser.Parse(dsl);

        result.Statements[0].Constraints.Should().Contain("offline");
    }

    [Fact]
    public void Parse_EntityReference_CapturesAtIdentifier()
    {
        var dsl = "TYPE: cli\nNAME: x\nGOAL: y\nlire @user_db";
        var result = _parser.Parse(dsl);

        result.Statements[0].EntityRefs.Should().Contain("user_db");
    }

    [Fact]
    public void Parse_CommentInline_AttachedToStatement()
    {
        var dsl = "TYPE: cli\nNAME: x\nGOAL: y\nfiltrer <mail> # filtre principal";
        var result = _parser.Parse(dsl);

        result.Statements[0].Comment.Should().Be("# filtre principal");
    }

    [Fact]
    public void Parse_UnclosedSlot_ReportsError()
    {
        var dsl = "TYPE: cli\nNAME: x\nGOAL: y\nfiltrer <mail";
        var result = _parser.Parse(dsl);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Message.Contains("slot", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_MissingHeader_ReportsError()
    {
        var dsl = "filtrer <mail>";
        var result = _parser.Parse(dsl);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Message.Contains("header", StringComparison.OrdinalIgnoreCase));
    }
}
