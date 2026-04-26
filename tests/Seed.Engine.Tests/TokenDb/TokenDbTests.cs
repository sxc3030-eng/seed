using FluentAssertions;
using Seed.Engine.TokenDb;
using Xunit;

namespace Seed.Engine.Tests.TokenDb;

public class TokenDbTests
{
    private readonly ITokenDb _db = new Engine.TokenDb.TokenDb();

    [Fact]
    public void GetVerbs_ForCli_Returns20Verbs()
    {
        var verbs = _db.GetVerbs("cli");
        verbs.Should().HaveCount(20);
        verbs.Select(v => v.Name).Should().Contain(new[] { "filtrer", "enregistrer", "alerter" });
    }

    [Fact]
    public void FindVerb_KnownVerb_ReturnsDefinition()
    {
        var verb = _db.FindVerb("cli", "filtrer");
        verb.Should().NotBeNull();
        verb!.Category.Should().Be("transformation");
        verb.Color.Should().Be("#06b6d4");
    }

    [Fact]
    public void FindVerb_UnknownVerb_ReturnsNull()
    {
        var verb = _db.FindVerb("cli", "non-existent-verb");
        verb.Should().BeNull();
    }

    [Fact]
    public void IsValidCombination_VerbAcceptsModifier_ReturnsTrue()
    {
        _db.IsValidCombination("cli", "filtrer", "pertinence").Should().BeTrue();
    }

    [Fact]
    public void IsValidCombination_VerbDoesNotAcceptModifier_ReturnsFalse()
    {
        _db.IsValidCombination("cli", "filtrer", "REST").Should().BeFalse();
    }

    [Fact]
    public void GetGrammar_UnknownType_ThrowsArgumentException()
    {
        var act = () => _db.GetGrammar("non-existent-type");
        act.Should().Throw<ArgumentException>();
    }
}
