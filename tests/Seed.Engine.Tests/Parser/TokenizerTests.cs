using FluentAssertions;
using Seed.Engine.Parser;
using Xunit;

namespace Seed.Engine.Tests.Parser;

public class TokenizerTests
{
    [Fact]
    public void Tokenize_SimpleStatement_ProducesExpectedTokens()
    {
        var dsl = "filtrer <mail>";
        var tokens = new Tokenizer().Tokenize(dsl).ToList();

        tokens.Should().SatisfyRespectively(
            t => { t.Type.Should().Be(LexTokenType.Identifier); t.Value.Should().Be("filtrer"); },
            t => t.Type.Should().Be(LexTokenType.Whitespace),
            t => t.Type.Should().Be(LexTokenType.SlotOpen),
            t => { t.Type.Should().Be(LexTokenType.Identifier); t.Value.Should().Be("mail"); },
            t => t.Type.Should().Be(LexTokenType.SlotClose),
            t => t.Type.Should().Be(LexTokenType.Eof)
        );
    }

    [Fact]
    public void Tokenize_QualifiedSlot_ProducesColonToken()
    {
        var dsl = "<db:sqlite>";
        var tokens = new Tokenizer().Tokenize(dsl).Where(t => t.Type != LexTokenType.Whitespace).ToList();

        tokens.Select(t => t.Type).Should().Equal(
            LexTokenType.SlotOpen,
            LexTokenType.Identifier,
            LexTokenType.Colon,
            LexTokenType.Identifier,
            LexTokenType.SlotClose,
            LexTokenType.Eof
        );
    }

    [Fact]
    public void Tokenize_AllOperators_RecognizesEachOne()
    {
        var dsl = "a → b & c | d";
        var tokens = new Tokenizer().Tokenize(dsl).Where(t => t.Type != LexTokenType.Whitespace).ToList();

        tokens.Select(t => t.Type).Should().Equal(
            LexTokenType.Identifier,
            LexTokenType.SeqArrow,
            LexTokenType.Identifier,
            LexTokenType.AmpersandPar,
            LexTokenType.Identifier,
            LexTokenType.PipeAlt,
            LexTokenType.Identifier,
            LexTokenType.Eof
        );
    }

    [Fact]
    public void Tokenize_CommentLine_EmitsHashAndConsumesRestAsIdentifier()
    {
        var dsl = "# this is a comment";
        var tokens = new Tokenizer().Tokenize(dsl).Where(t => t.Type != LexTokenType.Whitespace).ToList();

        tokens[0].Type.Should().Be(LexTokenType.Hash);
        tokens[0].Value.Should().Be("# this is a comment");
        tokens[1].Type.Should().Be(LexTokenType.Eof);
    }

    [Fact]
    public void Tokenize_BangAndAtAndQuestion_AllRecognized()
    {
        var dsl = "!offline ?TBD @user";
        var tokens = new Tokenizer().Tokenize(dsl).Where(t => t.Type != LexTokenType.Whitespace).ToList();

        tokens.Select(t => t.Type).Should().Equal(
            LexTokenType.Bang,
            LexTokenType.Identifier,
            LexTokenType.Question,
            LexTokenType.Identifier,
            LexTokenType.AtSign,
            LexTokenType.Identifier,
            LexTokenType.Eof
        );
    }

    [Fact]
    public void Tokenize_MultilineWithNewlines_EmitsNewlineTokens()
    {
        var dsl = "filtrer <mail>\nenregistrer <db>";
        var tokens = new Tokenizer().Tokenize(dsl)
            .Where(t => t.Type != LexTokenType.Whitespace)
            .ToList();

        tokens.Should().Contain(t => t.Type == LexTokenType.Newline);
    }
}
