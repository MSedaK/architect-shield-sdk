using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ArchitectShieldSDK
{
    public static class DiagnosticIds
    {
        public const string SecurityIP = "ARCH001";
        public const string LabelSystem = "ARCH002";
        public const string AsyncNaming = "ARCH003";
        public const string ResultPattern = "ARCH004";
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ArchitectShieldSDKAnalyzer : DiagnosticAnalyzer
    {
        // Diagnostic Descriptors
        private static readonly DiagnosticDescriptor Arch001Rule = new DiagnosticDescriptor("ARCH001", "Security: Hardcoded IP", "Security risk: IP address '{0}' detected.", "Security", DiagnosticSeverity.Error, isEnabledByDefault: true);
        private static readonly DiagnosticDescriptor Arch002Rule = new DiagnosticDescriptor("ARCH002", "Architecture: Label System", "Use corporate label '{0}' instead of raw string.", "Design", DiagnosticSeverity.Warning, isEnabledByDefault: true);
        private static readonly DiagnosticDescriptor Arch003Rule = new DiagnosticDescriptor("ARCH003", "Naming: Async Suffix", "Method '{0}' returns Task but lacks 'Async' suffix.", "Naming", DiagnosticSeverity.Warning, isEnabledByDefault: true);
        private static readonly DiagnosticDescriptor Arch004Rule = new DiagnosticDescriptor("ARCH004", "Architecture: Result Pattern", "Method '{0}' with [WrapResult] must return Result<T>.", "Architecture", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Arch001Rule, Arch002Rule, Arch003Rule, Arch004Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(startContext =>
            {
                var labels = LoadLabels(startContext.Options.AdditionalFiles);

                startContext.RegisterSyntaxNodeAction(ctx => AnalyzeStrings(ctx, labels), SyntaxKind.StringLiteralExpression);

                startContext.RegisterSyntaxNodeAction(AnalyzeMethods, SyntaxKind.MethodDeclaration);
            });
        }

        private Dictionary<string, string> LoadLabels(ImmutableArray<AdditionalText> files)
        {
            var dictionary = new Dictionary<string, string>();
            var file = files.FirstOrDefault(f => f.Path.EndsWith("labels.json", StringComparison.OrdinalIgnoreCase));

            if (file == null) return dictionary;

            var content = file.GetText()?.ToString();
            if (string.IsNullOrEmpty(content)) return dictionary;

            var lines = content.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = line.Split(new[] { ':' }, 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim(' ', '{', '}', '"', '\r', '\n', '\t');
                    var value = parts[1].Trim(' ', '{', '}', '"', '\r', '\n', '\t');
                    if (!string.IsNullOrEmpty(key)) dictionary[key] = value;
                }
            }
            return dictionary;
        }

        private void AnalyzeStrings(SyntaxNodeAnalysisContext context, Dictionary<string, string> labels)
        {
            var stringLiteral = (LiteralExpressionSyntax)context.Node;
            var val = stringLiteral.Token.ValueText;
            if (string.IsNullOrWhiteSpace(val)) return;

            // ARCH001: IP Check
            if (System.Text.RegularExpressions.Regex.IsMatch(val, @"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$"))
            {
                context.ReportDiagnostic(Diagnostic.Create(Arch001Rule, stringLiteral.GetLocation(), val));
                return;
            }

            // ARCH002: Label Matching
            var match = labels.FirstOrDefault(x => x.Value.Equals(val, StringComparison.OrdinalIgnoreCase));
            if (match.Key != null)
            {
                context.ReportDiagnostic(Diagnostic.Create(Arch002Rule, stringLiteral.GetLocation(), match.Key));
            }
        }

        private void AnalyzeMethods(SyntaxNodeAnalysisContext context)
        {
            var method = (MethodDeclarationSyntax)context.Node;
            var symbol = context.SemanticModel.GetDeclaredSymbol(method);
            if (symbol == null) return;

            var methodName = method.Identifier.Text;
            var returnType = symbol.ReturnType;

            // ARCH003: Async Suffix Check
            if (returnType != null && (returnType.Name.Contains("Task") || returnType.Name.Contains("ValueTask")) && !methodName.EndsWith("Async"))
            {
                context.ReportDiagnostic(Diagnostic.Create(Arch003Rule, method.Identifier.GetLocation(), methodName));
            }

            // ARCH004: Result Pattern 
            var hasWrapResult = symbol.GetAttributes().Any(a => a.AttributeClass?.Name == "WrapResultAttribute" || a.AttributeClass?.Name == "WrapResult");
            if (hasWrapResult && !returnType.Name.StartsWith("Result"))
            {
                context.ReportDiagnostic(Diagnostic.Create(Arch004Rule, method.ReturnType.GetLocation(), methodName));
            }
        }
    }
}