using System.Text;
using Seed.Engine.Models;

namespace Seed.Engine.Composer;

public sealed class Composer : IComposer
{
    private const string ArrowSeq = " → ";
    private const string ArrowPar = " & ";
    private const string ArrowAlt = " | ";

    public string Compose(ComposerInput input)
    {
        var sb = new StringBuilder();
        sb.Append("TYPE: ").AppendLine(input.Header.Type);
        sb.Append("NAME: ").AppendLine(input.Header.Name);
        sb.Append("GOAL: ").AppendLine(input.Header.Goal);

        for (var i = 0; i < input.Statements.Count; i++)
        {
            var stmt = input.Statements[i];
            AppendStatement(sb, stmt);

            if (stmt.LinkToNext is not null && i + 1 < input.Statements.Count)
            {
                sb.Append(stmt.LinkToNext.Value switch
                {
                    LinkType.Seq => ArrowSeq,
                    LinkType.Par => ArrowPar,
                    LinkType.Alt => ArrowAlt,
                    _ => " "
                });
            }
            else if (i + 1 < input.Statements.Count)
            {
                sb.AppendLine();
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static void AppendStatement(StringBuilder sb, ComposerStatement stmt)
    {
        foreach (var c in stmt.Constraints) sb.Append('!').Append(c).Append(' ');
        sb.Append(stmt.Verb);

        if (!string.IsNullOrEmpty(stmt.Target)) sb.Append(' ').Append('<').Append(stmt.Target).Append('>');

        foreach (var m in stmt.Modifiers)
        {
            sb.Append(' ').Append('<');
            if (!string.IsNullOrEmpty(m.Key)) sb.Append(m.Key).Append(':');
            sb.Append(m.Value).Append('>');
        }

        foreach (var e in stmt.EntityRefs) sb.Append(' ').Append('@').Append(e);

        if (!string.IsNullOrEmpty(stmt.Comment)) sb.Append("     ").Append(stmt.Comment);
    }
}
