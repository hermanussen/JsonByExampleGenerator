using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Emit;
using System.IO;
using System.Reflection;

namespace JsonByExampleGenerator.Generator
{
    [Generator]
    public class Generator : ISourceGenerator
    {

        public void Execute(SourceGeneratorContext context)
        {
            try
            {
                context.AddSource("CompileTimeExecutorAttribute", SourceText.From(@"namespace System
                            {
                                public static class JsonByExampleGenerator
                                {
                                    public static void RegisterByExample(this Type type, string fileName) {}
                                }
                            }", Encoding.UTF8));

                // retreive the populated receiver 
                if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
                    return;

                Compilation compilation = context.Compilation;

                foreach (InvocationExpressionSyntax invocation in receiver.CandidateInvocations)
                {
                    string typeName = invocation.DescendantNodes().OfType<TypeOfExpressionSyntax>().FirstOrDefault()?.Type?.ToString();

                    if(!string.IsNullOrWhiteSpace(typeName))
                    {
                        SemanticModel model = compilation.GetSemanticModel(invocation.SyntaxTree);
                        var symbol = model.GetDeclaredSymbol(invocation.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault()) as ITypeSymbol;

                        string namespaceName = symbol.ContainingNamespace.ToDisplayString();
                        if(string.IsNullOrWhiteSpace(namespaceName))
                        {
                            namespaceName = symbol.ContainingAssembly.GlobalNamespace.ToDisplayString();
                        }

                        string generatedClass = $@"namespace {namespaceName}
                            {{
                                public partial class {typeName}
                                {{
                                    
                                }}
                            }}";
                        context.AddSource($"{namespaceName}_{symbol.Name}_{symbol.ToString()}.gen.cs",
                            SourceText.From(generatedClass, Encoding.UTF8));

                        // string message = $"Invocation found: {typeName} - {generatedClass}";
                        // context.ReportDiagnostic(Diagnostic.Create(
                        //     new DiagnosticDescriptor(
                        //         "SI0000",
                        //         message,
                        //         message,
                        //         "JsonByExampleGenerator",
                        //         DiagnosticSeverity.Error,
                        //         isEnabledByDefault: true), 
                        //     Location.None));
                    }
                }
            }
            catch(Exception ex)
            {
                string message = $"Exception: {ex.Message} - {ex.StackTrace}";
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "SI0000",
                        message,
                        message,
                        "JsonByExampleGenerator",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true), 
                    Location.None));
            }
        }

        public void Initialize(InitializationContext context)
        {
            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        /// <summary>
        /// Created on demand before each generation pass
        /// </summary>
        class SyntaxReceiver : ISyntaxReceiver
        {
            public List<InvocationExpressionSyntax> CandidateInvocations { get; } = new List<InvocationExpressionSyntax>();

            /// <summary>
            /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
            /// </summary>
            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is InvocationExpressionSyntax invocationExpressionSyntax)
                {
                    var methodName = invocationExpressionSyntax
                        .DescendantNodes()
                        .OfType<MemberAccessExpressionSyntax>()
                        .FirstOrDefault()
                        ?.ChildNodes()
                        .OfType<IdentifierNameSyntax>()
                        .LastOrDefault()
                        ?.Identifier
                        .Text;
                    if(methodName == "RegisterByExample")
                    {
                        CandidateInvocations.Add(invocationExpressionSyntax);
                    }
                }
            }
        }
    }
}