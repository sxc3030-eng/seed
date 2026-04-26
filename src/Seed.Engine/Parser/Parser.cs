using Seed.Engine.Models;

namespace Seed.Engine.Parser;

public sealed class Parser : IParser
{
    private readonly Tokenizer _tokenizer = new();

    public ParseResult Parse(string dsl)
    {
        var result = new ParseResult();
        var tokens = _tokenizer.Tokenize(dsl)
            .Where(t => t.Type != LexTokenType.Whitespace)
            .ToList();

        var pos = 0;

        if (!TryParseHeader(tokens, ref pos, result))
        {
            result.Errors.Add(new ParseError { Message = "Missing or invalid project header (TYPE/NAME/GOAL)", Line = 1, Column = 1 });
            return result;
        }

        ParseStatements(tokens, ref pos, result);
        return result;
    }

    private static bool TryParseHeader(List<LexToken> tokens, ref int pos, ParseResult result)
    {
        var fields = new Dictionary<string, string>();
        var savedPos = pos;

        for (var i = 0; i < 3; i++)
        {
            SkipNewlines(tokens, ref pos);
            if (pos >= tokens.Count || tokens[pos].Type != LexTokenType.Identifier) break;

            var key = tokens[pos].Value.ToUpperInvariant();
            if (key is not ("TYPE" or "NAME" or "GOAL")) break;
            pos++;

            if (pos >= tokens.Count || tokens[pos].Type != LexTokenType.Colon) { pos = savedPos; return false; }
            pos++;

            var valueParts = new List<string>();
            while (pos < tokens.Count && tokens[pos].Type != LexTokenType.Newline && tokens[pos].Type != LexTokenType.Eof)
            {
                valueParts.Add(tokens[pos].Value);
                pos++;
            }

            fields[key] = string.Join(" ", valueParts).Trim();
        }

        if (!fields.ContainsKey("TYPE") || !fields.ContainsKey("NAME") || !fields.ContainsKey("GOAL"))
        {
            pos = savedPos;
            return false;
        }

        result.Header = new ProjectHeader
        {
            Type = fields["TYPE"],
            Name = fields["NAME"],
            Goal = fields["GOAL"]
        };
        return true;
    }

    private static void ParseStatements(List<LexToken> tokens, ref int pos, ParseResult result)
    {
        var nextId = 1;
        AstStatement? previous = null;
        LinkType? pendingLink = null;

        while (pos < tokens.Count && tokens[pos].Type != LexTokenType.Eof)
        {
            SkipNewlines(tokens, ref pos);
            if (pos >= tokens.Count || tokens[pos].Type == LexTokenType.Eof) break;

            if (tokens[pos].Type is LexTokenType.Hash or LexTokenType.DoubleHash)
            {
                pos++;
                continue;
            }

            var stmt = TryParseStatement(tokens, ref pos, $"s{nextId}", result);
            if (stmt is null) { pos++; continue; }

            nextId++;

            if (previous is not null && pendingLink is not null)
            {
                previous.Links.Add(new Link { To = stmt.Id, Type = pendingLink.Value });
            }

            result.Statements.Add(stmt);
            previous = stmt;

            SkipNewlines(tokens, ref pos);

            pendingLink = pos < tokens.Count
                ? tokens[pos].Type switch
                {
                    LexTokenType.SeqArrow => LinkType.Seq,
                    LexTokenType.AmpersandPar => LinkType.Par,
                    LexTokenType.PipeAlt => LinkType.Alt,
                    _ => null
                }
                : null;

            if (pendingLink is not null) pos++;
        }
    }

    private static AstStatement? TryParseStatement(List<LexToken> tokens, ref int pos, string id, ParseResult result)
    {
        var constraints = new List<string>();

        while (pos < tokens.Count && tokens[pos].Type == LexTokenType.Bang)
        {
            pos++;
            if (pos < tokens.Count && tokens[pos].Type == LexTokenType.Identifier)
            {
                constraints.Add(tokens[pos].Value);
                pos++;
            }
        }

        if (pos >= tokens.Count || tokens[pos].Type != LexTokenType.Identifier) return null;

        var verb = tokens[pos].Value;
        pos++;

        string target = string.Empty;
        var modifiers = new List<Modifier>();
        var entityRefs = new List<string>();
        var modifierLine = tokens[pos > 0 ? pos - 1 : 0].Line;

        while (pos < tokens.Count && tokens[pos].Type == LexTokenType.SlotOpen && tokens[pos].Line == modifierLine)
        {
            pos++;
            if (pos >= tokens.Count)
            {
                result.Errors.Add(new ParseError { Message = "Unclosed slot at end of input", Line = modifierLine, Column = 0 });
                return null;
            }

            var first = tokens[pos];
            if (first.Type != LexTokenType.Identifier)
            {
                result.Errors.Add(new ParseError { Message = "Expected identifier after slot opening '<'", Line = first.Line, Column = first.Column });
                return null;
            }
            pos++;

            string? key = null;
            string value = first.Value;

            if (pos < tokens.Count && tokens[pos].Type == LexTokenType.Colon)
            {
                pos++;
                if (pos >= tokens.Count || tokens[pos].Type != LexTokenType.Identifier)
                {
                    result.Errors.Add(new ParseError { Message = "Expected identifier after ':' in slot", Line = first.Line, Column = first.Column });
                    return null;
                }
                key = first.Value;
                value = tokens[pos].Value;
                pos++;
            }

            if (pos >= tokens.Count || tokens[pos].Type != LexTokenType.SlotClose)
            {
                result.Errors.Add(new ParseError { Message = $"Unclosed slot '<{first.Value}'", Line = first.Line, Column = first.Column });
                return null;
            }
            pos++;

            if (string.IsNullOrEmpty(target) && key is null) target = value;
            else modifiers.Add(new Modifier { Key = key, Value = value });
        }

        while (pos < tokens.Count && tokens[pos].Type == LexTokenType.AtSign && tokens[pos].Line == modifierLine)
        {
            pos++;
            if (pos < tokens.Count && tokens[pos].Type == LexTokenType.Identifier)
            {
                entityRefs.Add(tokens[pos].Value);
                pos++;
            }
        }

        string? comment = null;
        if (pos < tokens.Count && tokens[pos].Type is LexTokenType.Hash or LexTokenType.DoubleHash && tokens[pos].Line == modifierLine)
        {
            comment = tokens[pos].Value;
            pos++;
        }

        return new AstStatement
        {
            Id = id,
            Verb = verb,
            Target = target,
            Modifiers = modifiers,
            Constraints = constraints,
            EntityRefs = entityRefs,
            Comment = comment
        };
    }

    private static void SkipNewlines(List<LexToken> tokens, ref int pos)
    {
        while (pos < tokens.Count && tokens[pos].Type == LexTokenType.Newline) pos++;
    }
}
