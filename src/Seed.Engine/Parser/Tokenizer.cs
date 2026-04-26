namespace Seed.Engine.Parser;

public sealed class Tokenizer
{
    public IEnumerable<LexToken> Tokenize(string source)
    {
        int line = 1, column = 1, pos = 0;

        while (pos < source.Length)
        {
            var c = source[pos];

            if (c == '\n')
            {
                yield return new LexToken(LexTokenType.Newline, "\n", line, column);
                line++;
                column = 1;
                pos++;
                continue;
            }

            if (c == '\r')
            {
                pos++;
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                yield return new LexToken(LexTokenType.Whitespace, c.ToString(), line, column);
                column++;
                pos++;
                continue;
            }

            if (c == '#')
            {
                var isDouble = pos + 1 < source.Length && source[pos + 1] == '#';
                var startCol = column;
                var startPos = pos;
                while (pos < source.Length && source[pos] != '\n') pos++;
                var text = source.Substring(startPos, pos - startPos);
                column += text.Length;
                yield return new LexToken(isDouble ? LexTokenType.DoubleHash : LexTokenType.Hash, text, line, startCol);
                continue;
            }

            switch (c)
            {
                case '<':
                    yield return new LexToken(LexTokenType.SlotOpen, "<", line, column); pos++; column++; continue;
                case '>':
                    yield return new LexToken(LexTokenType.SlotClose, ">", line, column); pos++; column++; continue;
                case ':':
                    yield return new LexToken(LexTokenType.Colon, ":", line, column); pos++; column++; continue;
                case '&':
                    yield return new LexToken(LexTokenType.AmpersandPar, "&", line, column); pos++; column++; continue;
                case '|':
                    yield return new LexToken(LexTokenType.PipeAlt, "|", line, column); pos++; column++; continue;
                case '!':
                    yield return new LexToken(LexTokenType.Bang, "!", line, column); pos++; column++; continue;
                case '?':
                    yield return new LexToken(LexTokenType.Question, "?", line, column); pos++; column++; continue;
                case '@':
                    yield return new LexToken(LexTokenType.AtSign, "@", line, column); pos++; column++; continue;
            }

            if (c == '\u2192') // → (Unicode right arrow U+2192)
            {
                yield return new LexToken(LexTokenType.SeqArrow, "→", line, column);
                pos++; column++; continue;
            }

            if (c == '-' && pos + 1 < source.Length && source[pos + 1] == '>')
            {
                yield return new LexToken(LexTokenType.SeqArrow, "->", line, column);
                pos += 2; column += 2; continue;
            }

            if (IsIdentifierChar(c))
            {
                var startCol = column;
                var startPos = pos;
                while (pos < source.Length && IsIdentifierChar(source[pos])) { pos++; column++; }
                var ident = source.Substring(startPos, pos - startPos);
                yield return new LexToken(LexTokenType.Identifier, ident, line, startCol);
                continue;
            }

            // Unknown character: skip silently for now (parser will catch context errors)
            pos++;
            column++;
        }

        yield return new LexToken(LexTokenType.Eof, string.Empty, line, column);
    }

    private static bool IsIdentifierChar(char c) =>
        char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.' || c == '/' ||
        c == 'é' || c == 'è' || c == 'ê' || c == 'à' || c == 'ç' || c == 'ô' || c == 'û' || c == 'î' || c == 'â';
}
