using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace JsonByExampleGenerator.Generator
{
    /// <summary>
    /// Visit string constants that have a specific attribute and contain json to be used as an example.
    /// </summary>
    public class InCodeSyntaxReceiver : ISyntaxReceiver
    {
        public List<KeyValuePair<string, string>> InCodeJsons { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is FieldDeclarationSyntax fieldDeclarationSyntax
                    && fieldDeclarationSyntax.AttributeLists.Count > 0)
            {
                foreach (VariableDeclaratorSyntax variable in fieldDeclarationSyntax.Declaration.Variables)
                {
                    var expression = variable.Initializer?.Value as LiteralExpressionSyntax;
                    var stringValue = expression?.Token.Text;
                    if(stringValue != null)
                    {
                        InCodeJsons.Add(new KeyValuePair<string, string>("Animal", stringValue));
                    }
                }
            }
        }
    }
}