using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace VitalSync.Analyzers;

public static class CommentScanner
{
    private static readonly ImmutableDictionary<string, Func<string, ImmutableArray<TextSpan>>> ScannersByFileType =
        new Dictionary<string, Func<string, ImmutableArray<TextSpan>>>(StringComparer.OrdinalIgnoreCase)
        {
            [".cs"] = CSharp,
            [".razor"] = Razor,
            [".cshtml"] = Razor,
            [".css"] = Css,
            [".js"] = JavaScript,
            [".json"] = JavaScript,
            [".csproj"] = Markup,
            [".props"] = Markup,
            [".targets"] = Markup,
            [".slnx"] = Markup,
            [".xml"] = Markup,
            [".html"] = Markup,
            [".yml"] = Shell,
            [".yaml"] = Shell,
            [".sh"] = Shell,
            [".ps1"] = PowerShell,
            [".psm1"] = PowerShell,
            [".psd1"] = PowerShell,
            [".md"] = Markdown,
            [".editorconfig"] = text => LinesStartingWith(text, "#;"),
            [".gitignore"] = text => LinesStartingWith(text, "#"),
            [".gitattributes"] = text => LinesStartingWith(text, "#"),
            [".dockerignore"] = text => LinesStartingWith(text, "#"),
            [".txt"] = _ => [],
            [".png"] = _ => [],
            ["LICENSE"] = _ => [],
        }.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly ImmutableDictionary<string, Func<string, ImmutableArray<TextSpan>>> ScannersByFenceLanguage =
        new Dictionary<string, Func<string, ImmutableArray<TextSpan>>>(StringComparer.OrdinalIgnoreCase)
        {
            [string.Empty] = _ => [],
            ["text"] = _ => [],
            ["mermaid"] = _ => [],
            ["csharp"] = CSharp,
            ["cs"] = CSharp,
            ["razor"] = Razor,
            ["css"] = Css,
            ["js"] = JavaScript,
            ["javascript"] = JavaScript,
            ["json"] = JavaScript,
            ["xml"] = Markup,
            ["html"] = Markup,
            ["yaml"] = Shell,
            ["yml"] = Shell,
            ["bash"] = Shell,
            ["sh"] = Shell,
            ["shell"] = Shell,
            ["powershell"] = PowerShell,
            ["pwsh"] = PowerShell,
        }.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);

    public static string FileType(string path)
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        var name = path.Substring(path.LastIndexOfAny(['/', '\\']) + 1);
        var dot = name.LastIndexOf('.');
        return dot < 0 ? name : name.Substring(dot);
    }

    public static bool IsKnownFileType(string path) => ScannersByFileType.ContainsKey(FileType(path));

    public static ImmutableArray<TextSpan> ForFile(string path, string text) =>
        ScannersByFileType.TryGetValue(FileType(path), out var scanner)
            ? scanner(text ?? throw new ArgumentNullException(nameof(text)))
            : throw new NotSupportedException($"No comment syntax is known for '{path}'.");

    public static IEnumerable<SyntaxTrivia> CSharpComments(SyntaxNode root) =>
        root is null
            ? throw new ArgumentNullException(nameof(root))
            : root.DescendantTrivia(descendIntoTrivia: false).Where(trivia =>
                trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
                || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)
                || trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));

    public static ImmutableArray<TextSpan> CSharp(string text) =>
        [.. CSharpComments(CSharpSyntaxTree.ParseText(text).GetRoot()).Select(trivia => trivia.Span)];

    public static ImmutableArray<TextSpan> Css(string text)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        var comments = ImmutableArray.CreateBuilder<TextSpan>();
        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            if (c is '"' or '\'')
            {
                i = SkipQuoted(text, i, '\\', doubledQuoteEscapes: false, endsAtLineBreak: true);
            }
            else if (StartsWithAt(text, i, "/*"))
            {
                comments.Add(new TextSpan(i, 2));
                i = SkipPast(text, i + 2, "*/");
            }
            else
            {
                i++;
            }
        }

        return comments.ToImmutable();
    }

    public static ImmutableArray<TextSpan> JavaScript(string text)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        var comments = ImmutableArray.CreateBuilder<TextSpan>();
        var templateDepths = new Stack<int>();
        var depth = 0;
        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            if (c is '"' or '\'')
            {
                i = SkipQuoted(text, i, '\\', doubledQuoteEscapes: false, endsAtLineBreak: true);
            }
            else if (c == '`')
            {
                i = SkipTemplate(text, i + 1, templateDepths, depth);
            }
            else if (c == '{')
            {
                depth++;
                i++;
            }
            else if (c == '}' && templateDepths.Count > 0 && templateDepths.Peek() == depth)
            {
                templateDepths.Pop();
                i = SkipTemplate(text, i + 1, templateDepths, depth);
            }
            else if (c == '}')
            {
                depth--;
                i++;
            }
            else if (StartsWithAt(text, i, "//"))
            {
                comments.Add(new TextSpan(i, 2));
                i = LineEnd(text, i);
            }
            else if (StartsWithAt(text, i, "/*"))
            {
                comments.Add(new TextSpan(i, 2));
                i = SkipPast(text, i + 2, "*/");
            }
            else
            {
                i++;
            }
        }

        return comments.ToImmutable();
    }

    public static ImmutableArray<TextSpan> Razor(string text)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        var comments = ImmutableArray.CreateBuilder<TextSpan>();
        var i = 0;
        while (i < text.Length)
        {
            if (StartsWithAt(text, i, "@*") && (i == 0 || text[i - 1] != '@'))
            {
                comments.Add(new TextSpan(i, 2));
                i = SkipPast(text, i + 2, "*@");
            }
            else if (StartsWithAt(text, i, "<!--"))
            {
                comments.Add(new TextSpan(i, 4));
                i = SkipPast(text, i + 4, "-->");
            }
            else
            {
                i++;
            }
        }

        return comments.ToImmutable();
    }

    public static ImmutableArray<TextSpan> Markup(string text)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        var comments = ImmutableArray.CreateBuilder<TextSpan>();
        var i = text.IndexOf("<!--", StringComparison.Ordinal);
        while (i >= 0)
        {
            comments.Add(new TextSpan(i, 4));
            i = text.IndexOf("<!--", SkipPast(text, i + 4, "-->"), StringComparison.Ordinal);
        }

        return comments.ToImmutable();
    }

    public static ImmutableArray<TextSpan> Shell(string text)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        var comments = ImmutableArray.CreateBuilder<TextSpan>();
        var quote = '\0';
        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            if (c == '\n')
            {
                quote = '\0';
            }
            else if (quote != '\0')
            {
                if (c == '\\' && quote == '"')
                {
                    i++;
                }
                else if (c == quote)
                {
                    quote = '\0';
                }
            }
            else if (c is '"' or '\'')
            {
                quote = c;
            }
            else if (c == '#' && (i == 0 || char.IsWhiteSpace(text[i - 1])))
            {
                comments.Add(new TextSpan(i, 1));
                i = LineEnd(text, i);
                continue;
            }

            i++;
        }

        return comments.ToImmutable();
    }

    public static ImmutableArray<TextSpan> PowerShell(string text)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        var comments = ImmutableArray.CreateBuilder<TextSpan>();
        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            if (IsHereStringStart(text, i))
            {
                i = SkipPast(text, i + 2, "\n" + text[i + 1] + "@");
            }
            else if (c == '\'')
            {
                i = SkipQuoted(text, i, '\0', doubledQuoteEscapes: true, endsAtLineBreak: false);
            }
            else if (c == '"')
            {
                i = SkipQuoted(text, i, '`', doubledQuoteEscapes: true, endsAtLineBreak: false);
            }
            else if (c == '`')
            {
                i += 2;
            }
            else if (StartsWithAt(text, i, "<#"))
            {
                comments.Add(new TextSpan(i, 2));
                i = SkipPast(text, i + 2, "#>");
            }
            else if (c == '#' && (i == 0 || char.IsWhiteSpace(text[i - 1]) || ";({)}|,=".IndexOf(text[i - 1]) >= 0))
            {
                comments.Add(new TextSpan(i, 1));
                i = LineEnd(text, i);
            }
            else
            {
                i++;
            }
        }

        return comments.ToImmutable();
    }

    public static ImmutableArray<TextSpan> LinesStartingWith(string text, string markers)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        if (markers is null)
        {
            throw new ArgumentNullException(nameof(markers));
        }

        var comments = ImmutableArray.CreateBuilder<TextSpan>();
        var lineStart = 0;
        while (lineStart < text.Length)
        {
            var i = lineStart;
            while (i < text.Length && text[i] is ' ' or '\t')
            {
                i++;
            }

            if (i < text.Length && markers.IndexOf(text[i]) >= 0)
            {
                comments.Add(new TextSpan(i, 1));
            }

            lineStart = LineEnd(text, lineStart) + 1;
        }

        return comments.ToImmutable();
    }

    public static ImmutableArray<TextSpan> Markdown(string text)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        var comments = ImmutableArray.CreateBuilder<TextSpan>();
        Func<string, ImmutableArray<TextSpan>>? fenceScanner = null;
        var fenceStart = 0;
        var lineStart = 0;
        while (lineStart < text.Length)
        {
            var lineEnd = LineEnd(text, lineStart);
            var line = text.Substring(lineStart, lineEnd - lineStart);
            var trimmed = line.Trim();
            var next = Math.Min(lineEnd + 1, text.Length);
            if (fenceScanner is null && trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                var language = trimmed.Substring(3).Trim();
                if (!ScannersByFenceLanguage.TryGetValue(language, out fenceScanner))
                {
                    throw new NotSupportedException($"No comment syntax is known for code blocks in '{language}'.");
                }

                fenceStart = next;
            }
            else if (fenceScanner is not null && trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                foreach (var span in fenceScanner(text.Substring(fenceStart, lineStart - fenceStart)))
                {
                    comments.Add(new TextSpan(fenceStart + span.Start, span.Length));
                }

                fenceScanner = null;
            }
            else if (fenceScanner is null)
            {
                AddProseComments(line, lineStart, comments);
            }

            lineStart = next;
        }

        return comments.ToImmutable();
    }

    private static void AddProseComments(string line, int offset, ImmutableArray<TextSpan>.Builder comments)
    {
        var i = 0;
        while (i < line.Length)
        {
            if (line[i] == '`')
            {
                var run = 0;
                while (i + run < line.Length && line[i + run] == '`')
                {
                    run++;
                }

                var close = line.IndexOf(new string('`', run), i + run, StringComparison.Ordinal);
                i = close < 0 ? i + run : close + run;
            }
            else if (StartsWithAt(line, i, "<!--"))
            {
                comments.Add(new TextSpan(offset + i, 4));
                i += 4;
            }
            else
            {
                i++;
            }
        }
    }

    private static int SkipTemplate(string text, int i, Stack<int> templateDepths, int depth)
    {
        while (i < text.Length)
        {
            if (text[i] == '\\')
            {
                i += 2;
            }
            else if (text[i] == '`')
            {
                return i + 1;
            }
            else if (StartsWithAt(text, i, "${"))
            {
                templateDepths.Push(depth);
                return i + 2;
            }
            else
            {
                i++;
            }
        }

        return text.Length;
    }

    private static int SkipQuoted(string text, int start, char escape, bool doubledQuoteEscapes, bool endsAtLineBreak)
    {
        var quote = text[start];
        var i = start + 1;
        while (i < text.Length)
        {
            var c = text[i];
            if (escape != '\0' && c == escape)
            {
                i += 2;
            }
            else if (c == quote && doubledQuoteEscapes && i + 1 < text.Length && text[i + 1] == quote)
            {
                i += 2;
            }
            else if (c == quote)
            {
                return i + 1;
            }
            else if (c == '\n' && endsAtLineBreak)
            {
                return i;
            }
            else
            {
                i++;
            }
        }

        return text.Length;
    }

    private static bool IsHereStringStart(string text, int i) =>
        i + 2 < text.Length
        && text[i] == '@'
        && text[i + 1] is '\'' or '"'
        && text[i + 2] is '\r' or '\n';

    private static bool StartsWithAt(string text, int i, string value) =>
        string.CompareOrdinal(text, i, value, 0, value.Length) == 0;

    private static int SkipPast(string text, int i, string terminator)
    {
        var end = text.IndexOf(terminator, i, StringComparison.Ordinal);
        return end < 0 ? text.Length : end + terminator.Length;
    }

    private static int LineEnd(string text, int i)
    {
        var end = text.IndexOf('\n', i);
        return end < 0 ? text.Length : end;
    }
}
