using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace JsonByExampleGenerator.Generator
{
    /// <summary>
    /// Visit string constants that have a specific attribute and contain json to be used as an example.
    /// </summary>
    public class InCodeSyntaxReceiver : ISyntaxReceiver
    {
        private static readonly string[] JsonExampleAttributeNames = { "JsonExample", "JsonExampleAttribute" };

        public List<KeyValuePair<string, string>> InCodeJsons { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is FieldDeclarationSyntax fieldDeclarationSyntax
                    && fieldDeclarationSyntax.AttributeLists.Count > 0)
            {
                var attribute = fieldDeclarationSyntax
                    .AttributeLists
                    .SelectMany(a => a.Attributes)
                    .Where(a => JsonExampleAttributeNames.Contains(a.Name.ToFullString()))
                    .FirstOrDefault();
                if(attribute == null)
                {
                    return;
                }

                var jsonName = attribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression?.ToString().Trim().Trim('\"');
                if(jsonName == null)
                {
                    return;
                }    

                foreach (VariableDeclaratorSyntax variable in fieldDeclarationSyntax.Declaration.Variables)
                {
                    var expression = variable.Initializer?.Value as LiteralExpressionSyntax;
                    var stringValue = expression?.Token.ValueText;
                    if(!string.IsNullOrWhiteSpace(stringValue))
                    {
                        InCodeJsons.Add(new KeyValuePair<string, string>(jsonName, stringValue!));
                    }
                }
            }
        }
    }
}