using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ArchitectShieldSDK
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ArchitectShieldSDKCodeFixProvider)), Shared]
    public class ArchitectShieldSDKCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(DiagnosticIds.AsyncNaming); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Add 'Async' suffix",
                    createChangedSolution: c => AddAsyncSuffixAsync(context.Document, declaration, c),
                    equivalenceKey: "AddAsyncSuffix"),
                diagnostic);
        }

        private async Task<Solution> AddAsyncSuffixAsync(Document document, MethodDeclarationSyntax methodDecl, CancellationToken cancellationToken)
        {
            var newName = methodDecl.Identifier.Text + "Async";

            var newIdentifier = SyntaxFactory.Identifier(newName);
            var newMethodDecl = methodDecl.WithIdentifier(newIdentifier);

            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(methodDecl, newMethodDecl);

            return document.WithSyntaxRoot(newRoot).Project.Solution;
        }
    }
}