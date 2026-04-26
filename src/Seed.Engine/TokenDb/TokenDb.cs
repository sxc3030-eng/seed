using System.Reflection;
using System.Text.Json;

namespace Seed.Engine.TokenDb;

public sealed class TokenDb : ITokenDb
{
    private readonly Dictionary<string, GrammarProfile> _grammars;

    public TokenDb()
    {
        _grammars = LoadEmbeddedGrammars();
    }

    public GrammarProfile GetGrammar(string projectType)
    {
        if (!_grammars.TryGetValue(projectType, out var grammar))
        {
            throw new ArgumentException($"Unknown project type: {projectType}", nameof(projectType));
        }
        return grammar;
    }

    public IReadOnlyList<VerbDefinition> GetVerbs(string projectType) => GetGrammar(projectType).Verbs;

    public VerbDefinition? FindVerb(string projectType, string verbName) =>
        GetGrammar(projectType).Verbs.FirstOrDefault(v => v.Name == verbName);

    public bool IsValidCombination(string projectType, string verbName, string modifierKey)
    {
        var verb = FindVerb(projectType, verbName);
        return verb is not null && verb.AcceptsModifiers.Contains(modifierKey);
    }

    private static Dictionary<string, GrammarProfile> LoadEmbeddedGrammars()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith("Seed.Engine.TokenDb.tokens.") && n.EndsWith(".json"));

        var grammars = new Dictionary<string, GrammarProfile>();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        foreach (var resourceName in resourceNames)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Resource not found: {resourceName}");
            var grammar = JsonSerializer.Deserialize<GrammarProfile>(stream, options)
                ?? throw new InvalidOperationException($"Failed to deserialize: {resourceName}");
            grammars[grammar.ProjectType] = grammar;
        }

        return grammars;
    }
}
