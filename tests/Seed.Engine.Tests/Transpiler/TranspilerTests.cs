using FluentAssertions;
using Seed.Engine.Models;
using Seed.Engine.Parser;
using Seed.Engine.Transpiler;
using Xunit;

namespace Seed.Engine.Tests.Transpiler;

public class TranspilerTests
{
    private readonly IParser _parser = new Engine.Parser.Parser();
    private readonly ITranspiler _transpiler = new Engine.Transpiler.Transpiler();

    [Fact]
    public void Transpile_HeaderAndStatements_BuildsCompleteDnaFile()
    {
        var dsl = "TYPE: cli\nNAME: mail-filter\nGOAL: filtrer mes mails par pertinence\nfiltrer <mail> <pertinence> → enregistrer <db>";
        var parsed = _parser.Parse(dsl);
        parsed.IsValid.Should().BeTrue();

        var dna = _transpiler.Transpile(parsed);

        dna.Version.Should().Be("1.0");
        dna.Header.Type.Should().Be("cli");
        dna.Header.Name.Should().Be("mail-filter");
        dna.Header.Goal.Should().Be("filtrer mes mails par pertinence");
        dna.Statements.Should().HaveCount(2);
        dna.Statements[0].Verb.Should().Be("filtrer");
        dna.Statements[0].Target.Should().Be("mail");
        dna.Statements[0].Modifiers.Should().Contain(m => m.Value == "pertinence");
        dna.Statements[0].Links.Should().HaveCount(1);
        dna.Statements[0].Links[0].Type.Should().Be(LinkType.Seq);
        dna.Statements[1].Verb.Should().Be("enregistrer");
    }

    [Fact]
    public void Transpile_PreservesConstraintsAndComments()
    {
        var dsl = "TYPE: cli\nNAME: x\nGOAL: y\n!offline enregistrer <db> # local only";
        var parsed = _parser.Parse(dsl);

        var dna = _transpiler.Transpile(parsed);

        dna.Statements[0].Constraints.Should().Contain("offline");
        dna.Statements[0].Comment.Should().Be("# local only");
    }

    [Fact]
    public void Transpile_InvalidParseResult_ThrowsInvalidOperationException()
    {
        var bad = new ParseResult { Errors = { new ParseError { Message = "fake" } } };
        var act = () => _transpiler.Transpile(bad);
        act.Should().Throw<InvalidOperationException>();
    }
}
