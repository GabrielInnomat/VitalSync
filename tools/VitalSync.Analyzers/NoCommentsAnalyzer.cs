using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace VitalSync.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoCommentsAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "VSY0001";

    private static readonly ImmutableHashSet<string> BuildCheckedFileTypes =
        ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, ".razor", ".cshtml", ".css", ".js");

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Comments are not allowed",
        "Remove this comment: VitalSync allows no comments of any kind",
        "Documentation",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Every file in VitalSync is free of comments. Knowledge that a comment would carry belongs in the code itself or in docs/.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Rule];

    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
        context.RegisterAdditionalFileAction(AnalyzeAdditionalFile);
    }

    private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
    {
        var tree = context.Tree;
        foreach (var comment in CommentScanner.CSharpComments(tree.GetRoot(context.CancellationToken)))
        {
            if (context.IsGeneratedCode && !IsWrittenInRazor(tree, comment.Span))
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, Location.Create(tree, comment.Span)));
        }
    }

    private static bool IsWrittenInRazor(SyntaxTree tree, TextSpan span)
    {
        if (tree.GetLineVisibility(span.Start) != LineVisibility.Visible)
        {
            return false;
        }

        var mapped = tree.GetMappedLineSpan(span);
        return mapped.HasMappedPath
            && (mapped.Path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
                || mapped.Path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase));
    }

    private static void AnalyzeAdditionalFile(AdditionalFileAnalysisContext context)
    {
        var file = context.AdditionalFile;
        if (!BuildCheckedFileTypes.Contains(CommentScanner.FileType(file.Path)))
        {
            return;
        }

        var text = file.GetText(context.CancellationToken);
        if (text is null)
        {
            return;
        }

        foreach (var span in CommentScanner.ForFile(file.Path, text.ToString()))
        {
            var location = Location.Create(file.Path, span, text.Lines.GetLinePositionSpan(span));
            context.ReportDiagnostic(Diagnostic.Create(Rule, location));
        }
    }
}
