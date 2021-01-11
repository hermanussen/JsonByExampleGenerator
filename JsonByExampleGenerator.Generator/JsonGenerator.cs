using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Emit;
using System.Reflection;
using Pluralize.NET;
using JsonByExampleGenerator.Generator.Models;
using System.Globalization;

namespace JsonByExampleGenerator.Generator
{
    [Generator]
    public class JsonGenerator : ISourceGenerator
    {
        private static readonly IPluralize _pluralizer = new Pluralizer();

        public void Execute(GeneratorExecutionContext context)
        {
            try
            {
                foreach(var jsonFile in context.AdditionalFiles.Where(f => f.Path.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase)))
                {
                    var jsonFileText = jsonFile.GetText(context.CancellationToken);
                    if(jsonFileText == null)
                    {
                        continue;
                    }

                    var json = JsonDocument.Parse(jsonFileText.ToString());

                    string namespaceName = context.Compilation?.AssemblyName ?? "JsonByExample";

                    var classModels = new List<ClassModel>();
                    var jsonElement = json.RootElement;
                    string rootTypeName = GetValidName(Path.GetFileNameWithoutExtension(jsonFile.Path).Replace(" ", string.Empty));
                    RenderType(context, classModels, jsonElement, namespaceName, rootTypeName);

                    var generatedClasses = classModels.Select(c => $@"
    public partial class {c.ClassName}
    {{
{string.Join("\r\n", c.Properties.Select(p => RenderProperty(p)))}
    }}");

                    string generatedCode = $@"#nullable disable
using System.Collections.Generic;

namespace {namespaceName}.Json
{{{string.Join("\r\n", generatedClasses)}
}}
#nullable enable";
                    context.AddSource($"{namespaceName}.gen.cs",
                        SourceText.From(generatedCode, Encoding.UTF8));
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
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

        private static string RenderProperty(PropertyModel p)
        {
            string setter = p.Init == null ? " set;" : string.Empty;
            string init = p.Init != null ? $" = {p.Init};" : string.Empty;
            return $"        public {p.PropertyType} {p.PropertyName} {{ get;{setter} }}{init}";
        }

        private static void RenderType(GeneratorExecutionContext context, List<ClassModel> classModels, JsonElement jsonElement, string namespaceName, string typeName)
        {
            var classModel = new ClassModel(typeName);

            if (jsonElement.ValueKind == JsonValueKind.Array)
            {
                var jsonArrayEnumerator = jsonElement.EnumerateArray();
                while (jsonArrayEnumerator.MoveNext())
                {
                    RenderType(context, classModels, jsonArrayEnumerator.Current, namespaceName, typeName);
                }

                return;
            }

            foreach (JsonProperty prop in jsonElement.EnumerateObject())
            {
                string propName = prop.Name;
                if (propName.Length > 0)
                {
                    if (!char.IsUpper(propName[0]))
                    {
                        propName = $"{char.ToUpper(propName[0], CultureInfo.InvariantCulture)}{propName.Substring(1)}";
                    }

                    PropertyModel propertyModel;

                    switch (prop.Value.ValueKind)
                    {
                        case JsonValueKind.Array:
                            {
                                string arrPropName = GetValidName(prop.Name);
                                RenderType(context, classModels, prop.Value, namespaceName, arrPropName);

                                propertyModel = new PropertyModel($"IList<{arrPropName}>", propName)
                                    {
                                        Init = $"new List<{arrPropName}>()"
                                    };
                                break;
                            }
                        case JsonValueKind.String: propertyModel = new PropertyModel("string", propName); break;
                        case JsonValueKind.Number: propertyModel = new PropertyModel("long", propName); break;
                        case JsonValueKind.False:
                        case JsonValueKind.True: propertyModel = new PropertyModel("bool", propName); break;
                        case JsonValueKind.Object:
                            {
                                string objectPropName = GetValidName(prop.Name);
                                RenderType(context, classModels, prop.Value, namespaceName, objectPropName);

                                propertyModel = new PropertyModel(objectPropName, propName);
                                break;
                            }
                        case JsonValueKind.Undefined:
                        case JsonValueKind.Null:
                        default: propertyModel = new PropertyModel("object", propName); break;
                    }

                    classModel.Properties.Add(propertyModel);
                }
            }

            var matchingClassModel = classModels.FirstOrDefault(c => string.Equals(c.ClassName, classModel.ClassName, StringComparison.InvariantCulture));
            if (matchingClassModel != null)
            {
                matchingClassModel.Merge(classModel);
            }
            else
            {
                classModels.Add(classModel);
            }
        }

        private static string GetValidName(string typeName)
        {
            if (_pluralizer.IsPlural(typeName))
            {
                typeName = _pluralizer.Singularize(typeName);
            }

            if (!char.IsUpper(typeName[0]))
            {
                typeName = $"{char.ToUpper(typeName[0], CultureInfo.InvariantCulture)}{typeName.Substring(1)}";
            }

            return typeName;
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }
    }
}