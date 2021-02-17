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
using Pluralize.NET;
using JsonByExampleGenerator.Generator.Models;
using System.Globalization;
using Scriban;
using JsonByExampleGenerator.Generator.Utils;
using System.Text.Json.Serialization;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace JsonByExampleGenerator.Generator
{
    /// <summary>
    /// Source generator that generates C# code based on an example json file.
    /// </summary>
    [Generator]
    public class JsonGenerator : ISourceGenerator
    {
        private static readonly string RuntimeVersion = Environment.Version.ToString();
        private static readonly string ToolName = typeof(JsonGenerator).Assembly.GetName().Name;
        private static readonly string ToolVersion = typeof(JsonGenerator).Assembly.GetName().Version.ToString();

        private static readonly IPluralize pluralizer = new Pluralizer();
        private static readonly char[] forbiddenCharacters = new[] { ' ', '-', ':', ';' };
        private static readonly Regex parseNumberFromPropertyName = new Regex("(.*Property)([0-9]+)", RegexOptions.Compiled);

        /// <summary>
        /// Executes the generator logic during compilation
        /// </summary>
        /// <param name="context">Generator context that contains info about the compilation</param>
        public void Execute(GeneratorExecutionContext context)
        {
            try
            {
                // The namespace of the code is determined by the assembly name of the compilation
                string namespaceName = context.Compilation?.AssemblyName ?? "JsonByExample";

                // Determine if the functionality for easy access to configuration should be enabled
                bool configEnabled = IsConfigurationEnabled(context);

                // A list of dependencies to be added to the using statements in the code generation
                var optionalDependencies = new List<string>();
                if (configEnabled)
                {
                    optionalDependencies.Add("Microsoft.Extensions.Configuration");
                }

                // Generate code that should only be generated once
                const string onlyOnceTemplatePath = "OnlyOnceTemplate.sbntxt";
                var onlyOnceTemplateContent = GetTemplateContent(context, onlyOnceTemplatePath)
                    ?? EmbeddedResource.GetContent(onlyOnceTemplatePath);
                var onlyOnceTemplate = Template.Parse(onlyOnceTemplateContent, onlyOnceTemplatePath);
                string onlyOnceGeneratedCode = onlyOnceTemplate.Render(new
                    {
                        NamespaceName = namespaceName,
                        RuntimeVersion = RuntimeVersion,
                        ToolName = ToolName,
                        ToolVersion = ToolVersion
                }, member => member.Name);

                // Add the generated code to the compilation
                context.AddSource($"{namespaceName}_onlyonce.gen.cs",
                    SourceText.From(onlyOnceGeneratedCode, Encoding.UTF8));

                // Resolve all json files that are added to the AdditionalFiles in the compilation
                var allJsonFiles = context.AdditionalFiles.Where(f => f.Path.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase));
                foreach (var jsonFile in allJsonFiles)
                {
                    var jsonFileText = jsonFile.GetText(context.CancellationToken);
                    if (jsonFileText == null)
                    {
                        continue;
                    }

                    var json = JsonDocument.Parse(jsonFileText.ToString());

                    // Determine the deeper namespace based on the path within the project and the file name
                    string deeperNamespaceName = GetDeeperNamespaceName(namespaceName, jsonFile);

                    // Read the json and build a list of models that can be used to generate classes
                    var classModels = new List<ClassModel>();
                    var jsonElement = json.RootElement;
                    string rootTypeName = GetValidName(Path.GetFileNameWithoutExtension(jsonFile.Path).Replace(" ", string.Empty), true);
                    ResolveTypeRecursive(context, classModels, jsonElement, rootTypeName);

                    // Attempt to find a Scriban template in the AdditionalFiles that has the same name as the json
                    string templateFileName = $"{Path.Combine(Path.GetDirectoryName(jsonFile.Path), Path.GetFileNameWithoutExtension(jsonFile.Path))}.sbntxt";
                    string? templateContent = GetTemplateContent(context, templateFileName);
                    if (string.IsNullOrWhiteSpace(templateContent))
                    {
                        var pathRoot = Path.GetPathRoot(jsonFile.Path);
                        var pathParts = Path.GetDirectoryName(jsonFile.Path).Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                        const string directorySbnFileName = "JsonByExampleTemplate.sbntxt";
                        do
                        {
                            var filePath = string.IsNullOrWhiteSpace(pathRoot)
                                ? Path.Combine(Path.Combine(pathParts), directorySbnFileName)
                                : Path.Combine(pathRoot, Path.Combine(pathParts.Skip(1).ToArray()), directorySbnFileName);
                            templateContent = GetTemplateContent(context, filePath);

                            if (!pathParts.Any())
                            {
                                break;
                            }
                            Array.Resize(ref pathParts, pathParts.Length - 1);
                        } while (string.IsNullOrWhiteSpace(templateContent));
                    }

                    Template template;
                    if (templateContent != null)
                    {
                        // Parse the template that is in the compilation
                        template = Template.Parse(templateContent, templateFileName);
                    }
                    else
                    {
                        // Fallback to the default template
                        const string defaultTemplatePath = "JsonByExampleTemplate.sbntxt";
                        template = Template.Parse(EmbeddedResource.GetContent(defaultTemplatePath), defaultTemplatePath);
                    }

                    if (context.Compilation != null)
                    {
                        FilterAndChangeBasedOnExistingCode(classModels, deeperNamespaceName, context.Compilation);
                    }

                    // Use Scriban to render the code using the model that was built
                    string generatedCode = template.Render(new
                    {
                        OptionalDependencies = optionalDependencies,
                        NamespaceName = deeperNamespaceName,
                        ConfigEnabled = configEnabled,
                        ClassModels = classModels,
                        RuntimeVersion = RuntimeVersion,
                        ToolName = ToolName,
                        ToolVersion = ToolVersion
                    }, member => member.Name);

                    // Add the generated code to the compilation
                    context.AddSource(GetSourceFileName(namespaceName, jsonFile.Path),
                        SourceText.From(generatedCode, Encoding.UTF8));
                }
            }
            catch (Exception ex)
            {
                // Report a diagnostic if an exception occurs while generating code; allows consumers to know what is going on
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

        /// <summary>
        /// Based on a namespace and a file, determine a proper namespace to generate classes in.
        /// </summary>
        /// <param name="namespaceName">Root namespace name</param>
        /// <param name="jsonFile">File to get namespace for</param>
        /// <returns></returns>
        private static string GetDeeperNamespaceName(string namespaceName, AdditionalText jsonFile)
        {
            string compilationPath = Path.GetFullPath(".");
            string relativePath = Path.GetFullPath(jsonFile.Path);
            if (relativePath.StartsWith(compilationPath))
            {
                relativePath = relativePath.Substring(compilationPath.Length);
            }

            if (relativePath.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase))
            {
                relativePath = relativePath.Substring(0, relativePath.Length - 5);
            }

            var deeperNamespaceParts = relativePath
                .Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries)
                .Select(r => GetValidName(r));

            return $"{namespaceName}.Json.{string.Join(".", deeperNamespaceParts)}";
        }

        /// <summary>
        /// Ensure a unique name for the added source file for the compilation.
        /// </summary>
        /// <param name="namespaceName">The namespace in code</param>
        /// <param name="path">The path of the json file</param>
        /// <returns></returns>
        private string GetSourceFileName(string namespaceName, string path)
        {
            var fullPath = Path.GetFullPath(path)
                .Replace(Path.GetFullPath("."), string.Empty)
                .Replace("/", "_")
                .Replace("\\", "_")
                .Replace(".", "_");

            return $"{namespaceName}_{fullPath.Trim('_')}.gen.cs";
        }

        /// <summary>
        /// Find the contents of a template file in the AdditionalFiles.
        /// </summary>
        /// <param name="context">The generator context, that contains additional files</param>
        /// <param name="templatePath">The path to the template</param>
        /// <returns></returns>
        private string? GetTemplateContent(GeneratorExecutionContext context, string templatePath)
        {
            return context
                .AdditionalFiles
                .Where(f =>
                    {
                        string compilationPath = Path.GetFullPath(".");
                        string fullPath = Path.GetFullPath(f.Path);
                        string fullTemplatePath = Path.GetFullPath(templatePath);

                        if (!fullPath.StartsWith(compilationPath)
                            || !fullTemplatePath.StartsWith(compilationPath))
                        {
                            // Don't allow paths outside the current compilation
                            return false;
                        }

                        return fullPath.Equals(fullTemplatePath, StringComparison.InvariantCultureIgnoreCase);
                    })
                .FirstOrDefault()
                ?.GetText(context.CancellationToken)
                ?.ToString();
        }

        /// <summary>
        /// If json properties are specified manually, filter them here, so they can be omitted when generating code.
        /// </summary>
        /// <param name="classModels">The list of class models to apply filtering to</param>
        /// <param name="namespaceName">The namespace, so we know what existing types to resolve</param>
        /// <param name="compilation">The compilation, so we can find existing types</param>
        private void FilterAndChangeBasedOnExistingCode(List<ClassModel> classModels, string namespaceName, Compilation compilation)
        {
            // Deal with classes that have been decorated with JsonRenamedFrom attribute
            // They must be renamed in the model
            var renamedAttributes = compilation
                .SyntaxTrees
                .SelectMany(s => s
                    .GetRoot()
                    .DescendantNodes()
                    .Where(d => d.IsKind(SyntaxKind.Attribute))
                    .OfType<AttributeSyntax>()
                    .Where(d => d.Name.ToString() == "JsonRenamedFrom")
                    .Select(d => new
                        {
                            Renamed = (d?.Parent?.Parent as ClassDeclarationSyntax)?.Identifier.ToString().Trim(),
                            From = d?.ArgumentList?.Arguments.FirstOrDefault()?.ToString().Trim().Trim('\"')
                        }))
                .Where(x => x.From != null
                            && x.Renamed != null
                            && compilation.GetTypeByMetadataName($"{namespaceName}.{x.Renamed}") != null)
                .ToList();
            foreach (var classModel in classModels)
            {
                var match = renamedAttributes.FirstOrDefault(r => r.From == classModel.ClassName);
                if (match != null)
                {
                    // Rename class
                    classModel.ClassName = match.Renamed ?? classModel.ClassName;

                    // Find all properties and update the type and init
                    foreach(var property in classModels.SelectMany(p => p.Properties).ToList())
                    {
                        // Update init statement, if applicable
                        if(property.Init == $"new List<{match.From}>()")
                        {
                            property.Init = $"new List<{classModel.ClassName}>()";
                        }

                        // Rename property type, if applicable
                        if(property.PropertyType == match.From)
                        {
                            property.PropertyType = classModel.ClassName;
                        }
                        else if (property.PropertyType == $"IList<{match.From}>")
                        {
                            property.PropertyType = $"IList<{classModel.ClassName}>";
                        }
                    }
                }
            }

            foreach (var classModel in classModels)
            {
                // Find a class in the current compilation that already exists
                var existingClass = compilation.GetTypeByMetadataName($"{namespaceName}.{classModel.ClassName}");
                if(existingClass != null)
                {
                    // Find all DataMember decorations
                    var jsonProperties = existingClass
                        .GetMembers()
                        .OfType<IPropertySymbol>()
                        .SelectMany(m => m
                            .GetAttributes()
                            .Where(a =>
                                string.Equals(nameof(DataMemberAttribute), a.AttributeClass?.Name, StringComparison.InvariantCulture)
                                || string.Equals("DataMember", a.AttributeClass?.Name, StringComparison.InvariantCulture))
                            .Select(a => a.NamedArguments.FirstOrDefault(x => x.Key == "Name").Value.Value?.ToString())
                            .Where(a => a != null));
                    if(jsonProperties != null)
                    {
                        // Remove properties that are already in the compilation; no need to generate them
                        classModel.Properties.RemoveAll(p => jsonProperties.Contains(p.PropertyNameOriginal));
                    }
                }
            }
        }

        /// <summary>
        /// Find out if Microsoft.Extensions.Configuration.Json is used.
        /// </summary>
        /// <param name="context">The generator execution context</param>
        /// <returns>True if Microsoft.Extensions.Configuration.Json is referenced from the assembly</returns>
        private static bool IsConfigurationEnabled(GeneratorExecutionContext context)
        {
            bool configEnabled = context.Compilation?.ReferencedAssemblyNames
                .Any(r => string.Equals("Microsoft.Extensions.Configuration.Json", r.Name, StringComparison.InvariantCulture))
                ?? false;
            configEnabled = configEnabled
                && (context.Compilation?.ReferencedAssemblyNames
                .Any(r => string.Equals("Microsoft.Extensions.Configuration.Binder", r.Name, StringComparison.InvariantCulture)) ?? false);
            return configEnabled;
        }

        /// <summary>
        /// Reads json and fills the classModels list with relevant type definitions.
        /// </summary>
        /// <param name="context">The source generator context</param>
        /// <param name="classModels">A list that needs to be populated with resolved types</param>
        /// <param name="jsonElement">The current json element that is being read</param>
        /// <param name="typeName">The current type name that is being read</param>
        private static void ResolveTypeRecursive(GeneratorExecutionContext context, List<ClassModel> classModels, JsonElement jsonElement, string typeName)
        {
            var classModel = new ClassModel(typeName);

            // Arrays should be enumerated and handled individually
            if (jsonElement.ValueKind == JsonValueKind.Array)
            {
                var jsonArrayEnumerator = jsonElement.EnumerateArray();
                while (jsonArrayEnumerator.MoveNext())
                {
                    ResolveTypeRecursive(context, classModels, jsonArrayEnumerator.Current, typeName);
                }

                return;
            }

            if (jsonElement.ValueKind == JsonValueKind.Object)
            {
                int orderCounter = 0;

                // Iterate the properties of the json element, they will become model properties
                foreach (JsonProperty prop in jsonElement.EnumerateObject())
                {
                    string propName = RenameIfDuplicateOrConflicting(GetValidName(prop.Name), classModel);
                    
                    if (propName.Length > 0)
                    {
                        PropertyModel propertyModel;

                        // The json value kind of the property determines how to map it to a C# type
                        switch (prop.Value.ValueKind)
                        {
                            case JsonValueKind.Array:
                                {
                                    string arrPropName = GetValidName(prop.Name, true);

                                    // Look at the first element in the array to determine the type of the array
                                    var arrEnumerator = prop.Value.EnumerateArray();
                                    if (arrEnumerator.MoveNext())
                                    {
                                        if (arrEnumerator.Current.ValueKind == JsonValueKind.Number)
                                        {
                                            arrPropName = FindBestNumericType(arrEnumerator.Current);
                                        }
                                        else if (arrEnumerator.Current.ValueKind == JsonValueKind.String)
                                        {
                                            arrPropName = FindBestStringType(arrEnumerator.Current);
                                        }
                                        else if (arrEnumerator.Current.ValueKind == JsonValueKind.True || arrEnumerator.Current.ValueKind == JsonValueKind.False)
                                        {
                                            arrPropName = "bool";
                                        }
                                        else
                                        {
                                            ResolveTypeRecursive(context, classModels, prop.Value, arrPropName);
                                        }

                                        propertyModel = new PropertyModel(prop.Name, $"IList<{arrPropName}>", propName, orderCounter++)
                                        {
                                            Init = $"new List<{arrPropName}>()"
                                        };
                                    }
                                    else
                                    {
                                        propertyModel = new PropertyModel(prop.Name, $"IList<object>", propName, orderCounter++)
                                        {
                                            Init = $"new List<object>()"
                                        };
                                    }

                                    break;
                                }
                            case JsonValueKind.String: propertyModel = new PropertyModel(prop.Name, FindBestStringType(prop.Value), propName, orderCounter++); break;
                            case JsonValueKind.Number: propertyModel = new PropertyModel(prop.Name, FindBestNumericType(prop.Value), propName, orderCounter++); break;
                            case JsonValueKind.False:
                            case JsonValueKind.True: propertyModel = new PropertyModel(prop.Name, "bool", propName, orderCounter++); break;
                            case JsonValueKind.Object:
                                {
                                    string objectPropName = GetValidName(prop.Name, true);

                                    // Create a separate type for objects
                                    ResolveTypeRecursive(context, classModels, prop.Value, objectPropName);

                                    propertyModel = new PropertyModel(prop.Name, objectPropName, propName, orderCounter++);
                                    break;
                                }
                            case JsonValueKind.Undefined:
                            case JsonValueKind.Null:
                            default: propertyModel = new PropertyModel(prop.Name, "object", propName, orderCounter++); break;
                        }

                        classModel.Properties.Add(propertyModel);
                    }
                }
            }

            // If there is already a model defined that matches by name, then we add any new properties by merging the models
            var matchingClassModel = classModels.FirstOrDefault(
                c => string.Equals(c.ClassName, classModel.ClassName, StringComparison.InvariantCulture));
            if (matchingClassModel != null)
            {
                matchingClassModel.Merge(classModel);
            }
            else
            {
                // No need to merge, just add the new class model
                classModels.Add(classModel);
            }
        }

        private static string RenameIfDuplicateOrConflicting(string propertyName, ClassModel classModel)
        {
            const string postFix = "Property";
            string newPropertyName = propertyName;
            if(string.Equals(propertyName, classModel.ClassName, StringComparison.InvariantCulture))
            {
                // Property name conflicts with class name, so add a postfix
                newPropertyName = $"{propertyName}{postFix}";
            }

            while(classModel.Properties.Any(p => p.PropertyName == newPropertyName))
            {
                var match = parseNumberFromPropertyName.Match(newPropertyName);
                if (match.Success)
                {
                    newPropertyName = $"{match.Groups[1].Value}{int.Parse(match.Groups[2].Value) + 1}";
                }
                else
                {
                    newPropertyName = $"{newPropertyName}2";
                }
            }

            return newPropertyName;
        }

        /// <summary>
        /// Based on the value specified, determine an appropriate numeric type.
        /// </summary>
        /// <param name="propertyValue">Example value of the property</param>
        /// <returns>The name of the numeric type</returns>
        private static string FindBestNumericType(JsonElement propertyValue)
        {
            if (propertyValue.TryGetInt32(out _))
            {
                return "int";
            }

            if (propertyValue.TryGetInt64(out _))
            {
                return "long";
            }

            if (propertyValue.TryGetDouble(out var doubleVal)
                && propertyValue.TryGetDecimal(out var decimalVal)
                && Convert.ToDecimal(doubleVal) == decimalVal)
            {
                return "double";
            }

            if (propertyValue.TryGetDecimal(out _))
            {
                return "decimal";
            }

            return "object";
        }

        /// <summary>
        /// Based on the value specified, determine if anything better than "string" can be used.
        /// </summary>
        /// <param name="current">Example value of the property</param>
        /// <returns>string or something better</returns>
        private static string FindBestStringType(JsonElement propertyValue)
        {
            if (propertyValue.TryGetDateTime(out _))
            {
                return "DateTime";
            }

            return "string";
        }

        /// <summary>
        /// Gets a name that is valid in C# and makes it Pascal-case.
        /// Optionally, it can singularize the name, so that a list property has a proper model class.
        /// E.g. Cars will have a model type of Car.
        /// </summary>
        /// <param name="typeName">The type name that is possibly not valid in C#</param>
        /// <param name="singularize">If true, the name will be singularized if it is plural</param>
        /// <returns>A valid C# Pascal-case name</returns>
        private static string GetValidName(string typeName, bool singularize = false)
        {
            // Make a plural form singular using Pluralize.NET
            if (singularize && pluralizer.IsPlural(typeName))
            {
                typeName = pluralizer.Singularize(typeName);
            }

            List<char> newTypeName = new List<char>();
            bool nextCharUpper = true;
            for(int i = 0; i < typeName.Length; i++)
            {
                // Strip spaces and other characters
                if (forbiddenCharacters.Contains(typeName[i]))
                {
                    nextCharUpper = true;
                    continue;
                }
                
                // Pascal casing
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

        /// <summary>
        /// Initialization of the generator; allows to setup visitors for syntax.
        /// </summary>
        /// <param name="context">Code generator context</param>
        public void Initialize(GeneratorInitializationContext context)
        {
            // No implementation needed here; the generator is entirely driven by use of AdditionalFiles
        }
    }
}