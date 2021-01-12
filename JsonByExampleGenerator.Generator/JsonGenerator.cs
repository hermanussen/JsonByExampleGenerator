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
                foreach (var jsonFile in context.AdditionalFiles.Where(f => f.Path.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase)))
                {
                    var jsonFileText = jsonFile.GetText(context.CancellationToken);
                    if(jsonFileText == null)
                    {
                        continue;
                    }

                    bool configEnabled = context.Compilation?.ReferencedAssemblyNames
                        .Any(r => string.Equals("Microsoft.Extensions.Configuration.Json", r.Name, StringComparison.InvariantCulture))
                        ?? false;
                    configEnabled = configEnabled
                        && (context.Compilation?.ReferencedAssemblyNames
                        .Any(r => string.Equals("Microsoft.Extensions.Configuration.Binder", r.Name, StringComparison.InvariantCulture)) ?? false);

                    var json = JsonDocument.Parse(jsonFileText.ToString());

                    string namespaceName = context.Compilation?.AssemblyName ?? "JsonByExample";

                    var classModels = new List<ClassModel>();
                    var jsonElement = json.RootElement;
                    string rootTypeName = GetValidName(Path.GetFileNameWithoutExtension(jsonFile.Path).Replace(" ", string.Empty), true);
                    RenderType(context, classModels, jsonElement, namespaceName, rootTypeName);

                    var generatedClasses = classModels.Select(c =>
                        {
                            string extensionMethod = string.Empty;
                            string configReadMethod = string.Empty;

                            if (configEnabled)
                            {

                                configReadMethod = $@"
        public static {c.ClassName} FromConfig([System.Diagnostics.CodeAnalysis.NotNull] IConfiguration config)
        {{
            return config.Get<{c.ClassName}>();
        }}";
                            }

                            string result = $@"
{extensionMethod}
    public partial class {c.ClassName}
    {{
{string.Join("\r\n", c.Properties.Select(p => RenderProperty(p)))}
{configReadMethod}
    }}";

                            return result;
    });

                    var optionalDependencies = configEnabled ? "\r\nusing Microsoft.Extensions.Configuration;" : string.Empty;

                    string generatedCode = $@"#nullable disable
using System.Collections.Generic;
using System.Text.Json.Serialization;{optionalDependencies}

namespace {namespaceName}.Json
{{{string.Join("\r\n", generatedClasses)}
}}
#nullable enable";
                    context.AddSource($"{namespaceName}_{rootTypeName}.gen.cs",
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

        private static string RenderProperty(PropertyModel propertyModel)
        {
            string init = propertyModel.Init != null ? $" = {propertyModel.Init};" : string.Empty;
            return $"[JsonPropertyName(\"{propertyModel.PropertyNameOriginal}\")]\r\n        public {propertyModel.PropertyType} {propertyModel.PropertyName} {{ get; set; }}{init}";
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
                string propName = GetValidName(prop.Name);
                if (propName.Length > 0)
                {
                    PropertyModel propertyModel;

                    switch (prop.Value.ValueKind)
                    {
                        case JsonValueKind.Array:
                            {
                                string arrPropName = GetValidName(prop.Name, true);

                                var arrEnumerator = prop.Value.EnumerateArray();
                                if(arrEnumerator.MoveNext())
                                {
                                    if (arrEnumerator.Current.ValueKind == JsonValueKind.Number)
                                    {
                                        arrPropName = "double";
                                    }
                                    else if (arrEnumerator.Current.ValueKind == JsonValueKind.String)
                                    {
                                        arrPropName = "string";
                                    }
                                    else if (arrEnumerator.Current.ValueKind == JsonValueKind.True || arrEnumerator.Current.ValueKind == JsonValueKind.False)
                                    {
                                        arrPropName = "bool";
                                    }
                                    else
                                    {
                                        RenderType(context, classModels, prop.Value, namespaceName, arrPropName);
                                    }

                                    propertyModel = new PropertyModel(prop.Name, $"IList<{arrPropName}>", propName)
                                        {
                                            Init = $"new List<{arrPropName}>()"
                                        };
                                }
                                else
                                {
                                    propertyModel = new PropertyModel(prop.Name, $"IList<object>", propName)
                                        {
                                            Init = $"new List<object>()"
                                        };
                                }

                                break;
                            }
                        case JsonValueKind.String: propertyModel = new PropertyModel(prop.Name, "string", propName); break;
                        case JsonValueKind.Number: propertyModel = new PropertyModel(prop.Name, "double", propName); break;
                        case JsonValueKind.False:
                        case JsonValueKind.True: propertyModel = new PropertyModel(prop.Name, "bool", propName); break;
                        case JsonValueKind.Object:
                            {
                                string objectPropName = GetValidName(prop.Name, true);
                                RenderType(context, classModels, prop.Value, namespaceName, objectPropName);

                                propertyModel = new PropertyModel(prop.Name, objectPropName, propName);
                                break;
                            }
                        case JsonValueKind.Undefined:
                        case JsonValueKind.Null:
                        default: propertyModel = new PropertyModel(prop.Name, "object", propName); break;
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

        private static string GetValidName(string typeName, bool singularize = false)
        {
            if (singularize && _pluralizer.IsPlural(typeName))
            {
                typeName = _pluralizer.Singularize(typeName);
            }

            List<char> newTypeName = new List<char>();
            bool nextCharUpper = true;
            for(int i = 0; i < typeName.Length; i++)
            {
                if (typeName[i] == ' ')
                {
                    nextCharUpper = true;
                    continue;
                }
                
                if (nextCharUpper)
                {
                    nextCharUpper = false;
                    if (!char.IsUpper(typeName[i]))
                    {
                        newTypeName.Add(char.ToUpper(typeName[i], CultureInfo.InvariantCulture));
                        continue;
                    }
                }

                newTypeName.Add(typeName[i]);
            }

            return new string(newTypeName.ToArray());
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }
    }
}