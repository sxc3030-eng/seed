namespace Seed.Engine.Parser;

public enum LexTokenType
{
    Identifier,        // verb name or bare word
    SlotOpen,          // <
    SlotClose,         // >
    Colon,             // :
    SeqArrow,          // →
    AmpersandPar,      // &
    PipeAlt,           // |
    Bang,              // !
    Question,          // ?
    AtSign,            // @
    Hash,              // # (comment marker)
    DoubleHash,        // ## (block comment marker)
    Newline,
    Eof,
    Whitespace
}

public sealed record LexToken(LexTokenType Type, string Value, int Line, int Column);

public sealed class ParseError
{
    public string Message { get; init; } = string.Empty;
    public int Line { get; init; }
    public int Column { get; init; }
}

public sealed class ParseResult
{
    public List<AstStatement> Statements { get; init; } = new();
    public Models.ProjectHeader Header { get; init; } = new();
    public List<ParseError> Errors { get; init; } = new();
    public bool IsValid => Errors.Count == 0;
}

public sealed class AstStatement
{
    public string Id { get; init; } = string.Empty;
    public string Verb { get; init; } = string.Empty;
    public string Target { get; init; } = string.Empty;
    public List<Models.Modifier> Modifiers { get; init; } = new();
    public List<string> Constraints { get; init; } = new();
    public List<string> EntityRefs { get; init; } = new();
    public string? Comment { get; init; }
    public List<Models.Link> Links { get; init; } = new();
}
