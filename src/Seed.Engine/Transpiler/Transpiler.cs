using Seed.Engine.Models;
using Seed.Engine.Parser;

namespace Seed.Engine.Transpiler;

public sealed class Transpiler : ITranspiler
{
    public DnaFile Transpile(ParseResult parseResult)
    {
        if (!parseResult.IsValid)
        {
            throw new InvalidOperationException(
                $"Cannot transpile invalid parse result. Errors: {string.Join("; ", parseResult.Errors.Select(e => e.Message))}");
        }

        var statements = parseResult.Statements
            .Select(ast => new Statement
            {
                Id = ast.Id,
                Verb = ast.Verb,
                Target = ast.Target,
                Modifiers = ast.Modifiers,
                Constraints = ast.Constraints,
                Comment = ast.Comment,
                Links = ast.Links
            })
            .ToList();

        return new DnaFile
        {
            Version = "1.0",
            Header = parseResult.Header,
            Statements = statements
        };
    }
}
