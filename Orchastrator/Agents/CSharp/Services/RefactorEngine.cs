using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace A3sist.Agents.CSharp.Services
{
    public class RefactorEngine
    {
        public async Task InitializeAsync()
        {
            // Initialize refactoring engine
            await Task.CompletedTask;
        }

        public async Task<string> RefactorCodeAsync(string code)
        {
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = await tree.GetRootAsync();

            // Example refactoring: Convert var to explicit type
            var nodes = root.DescendantNodes().OfType<LocalDeclarationStatementSyntax>();

            foreach (var node in nodes)
            {
                if (node.Declaration.Variables.Count == 1)
                {
                    var variable = node.Declaration.Variables[0];
                    if (variable.Initializer != null)
                    {
                        var typeInfo = await GetTypeInfoAsync(tree, variable.Initializer.Value);
                        if (!typeInfo.Type.IsErrorType)
                        {
                            var newDeclaration = node.ReplaceNode(
                                variable,
                                variable.WithType(SyntaxFactory.ParseTypeName(typeInfo.Type.ToDisplayString()))
                            );

                            root = root.ReplaceNode(node, newDeclaration);
                        }
                    }
                }
            }

            return root.ToFullString();
        }

        private async Task<TypeInfo> GetTypeInfoAsync(SyntaxTree tree, ExpressionSyntax expression)
        {
            var compilation = CSharpCompilation.Create("Analysis")
                .AddSyntaxTrees(tree)
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var semanticModel = compilation.GetSemanticModel(tree);
            return await semanticModel.GetTypeInfoAsync(expression);
        }

        public async Task ShutdownAsync()
        {
            // Clean up resources
            await Task.CompletedTask;
        }
    }
}