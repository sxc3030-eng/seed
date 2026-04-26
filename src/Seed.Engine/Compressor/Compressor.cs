using System.Text;
using Seed.Engine.Models;

namespace Seed.Engine.Compressor;

public sealed class Compressor : ICompressor
{
    public string Compress(DnaFile dna)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(dna.Header.Type)) sb.Append("TYPE: ").AppendLine(dna.Header.Type);
        if (!string.IsNullOrEmpty(dna.Header.Name)) sb.Append("NAME: ").AppendLine(dna.Header.Name);
        if (!string.IsNullOrEmpty(dna.Header.Goal)) sb.Append("GOAL: ").AppendLine(dna.Header.Goal);

        var idToStatement = dna.Statements.ToDictionary(s => s.Id);
        var visited = new HashSet<string>();

        foreach (var stmt in dna.Statements)
        {
            if (visited.Contains(stmt.Id)) continue;
            EmitChain(sb, stmt, idToStatement, visited);
            sb.AppendLine();
        }

        return Normalize(sb.ToString().TrimEnd());
    }

    private static void EmitChain(StringBuilder sb, Statement stmt, Dictionary<string, Statement> all, HashSet<string> visited)
    {
        EmitStatement(sb, stmt);
        visited.Add(stmt.Id);

        foreach (var link in stmt.Links)
        {
            if (!all.TryGetValue(link.To, out var next) || visited.Contains(next.Id)) continue;
            sb.Append(link.Type switch
            {
                LinkType.Seq => " → ",
                LinkType.Par => " & ",
                LinkType.Alt => " | ",
                _ => " "
            });
            EmitChain(sb, next, all, visited);
        }
    }

    private static void EmitStatement(StringBuilder sb, Statement stmt)
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
    }

    private static string Normalize(string s)
    {
        var lines = s.Split('\n').Select(line => System.Text.RegularExpressions.Regex.Replace(line.TrimEnd(), @" {2,}", " "));
        return string.Join("\n", lines);
    }
}
