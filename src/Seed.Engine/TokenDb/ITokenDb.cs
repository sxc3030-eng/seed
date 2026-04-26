namespace Seed.Engine.TokenDb;

public interface ITokenDb
{
    GrammarProfile GetGrammar(string projectType);
    IReadOnlyList<VerbDefinition> GetVerbs(string projectType);
    VerbDefinition? FindVerb(string projectType, string verbName);
    bool IsValidCombination(string projectType, string verbName, string modifierKey);
}
