using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Abstractions;

namespace JsonByExampleGenerator.Tests
{
    public class TestsBase
    {
        private class AdditionalTextJson : AdditionalText
        {
            private readonly string _path;
            private readonly string _text;

            public AdditionalTextJson(string path, string text)
            {
                _path = path;
                _text = text;
            }

            public override string Path
            {
                get
                {
                    return _path;
                }
            }

            public override SourceText GetText(CancellationToken cancellationToken = default)
            {
                return SourceText.From(_text);
            }
        }

        private readonly ITestOutputHelper _output;
        private static List<MetadataReference>? _metadataReferences;
        private static readonly object Lock = new object();

        public TestsBase(ITestOutputHelper output)
        {
            _output = output;
        }

        private static List<MetadataReference> MetadataReferences
        {
            get
            {
                lock (Lock)
                {
                    if (_metadataReferences == null)
                    {
                        _metadataReferences = new List<MetadataReference>();
                        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                        foreach (var assembly in assemblies)
                        {
                            if (!assembly.IsDynamic)
                            {
                                _metadataReferences.Add(MetadataReference.CreateFromFile(assembly.Location));
                            }
                        }
                    }
                }

                return _metadataReferences;
            }
        }

        protected string? RunTest(Compilation compilation, List<Diagnostic>? diagnostics = null)
        {
            if (compilation == null)
            {
                throw new ArgumentException($"Argument {nameof(compilation)} must not be null");
            }

            using var memoryStream = new MemoryStream();
            EmitResult result = compilation.Emit(memoryStream);

            if (result.Success)
            {
                memoryStream.Seek(0, SeekOrigin.Begin);
                Assembly assembly = Assembly.Load(memoryStream.ToArray());
                Type? testClassType = assembly.GetType("Example.Test");
                var method = testClassType?.GetMethod("RunTest") ?? testClassType?.GetMethod("RunTestAsync");
                if(method == null)
                {
                    return "-- could not find test method --";
                }

                var resultObj = method.Invoke(null, Array.Empty<object>());
                if (resultObj is not string stringResult)
                {
                    return "-- result was not a string --";
                }

                _output.WriteLine($"Generated test output:\r\n===\r\n{stringResult}\r\n===\r\n");

                return stringResult;
            }

            if (diagnostics == null)
            {
                Assert.False(true,
                    $"Compilation did not succeed:\r\n{string.Join("\r\n", result.Diagnostics.Select(d => $"{Enum.GetName(typeof(DiagnosticSeverity), d.Severity)} ({d.Location}) - {d.GetMessage()}"))}");
            }
            else
            {
                diagnostics.AddRange(result.Diagnostics);
            }

            return null;
        }

        protected Compilation GetGeneratedOutput(string source, IDictionary<string, string> additionalFilesAndContents, List<Diagnostic>? diagnostics = null)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);

            var references = MetadataReferences;

            var additionalTexts = additionalFilesAndContents.Select(i => new AdditionalTextJson(i.Key, i.Value)).Cast<AdditionalText>();

            var compilation = CSharpCompilation.Create(
                    "TestImplementation",
                    new[] { syntaxTree },
                    references,
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            ISourceGenerator generator = new Generator.JsonGenerator();

            CSharpGeneratorDriver.Create(generator)
                .AddAdditionalTexts(ImmutableArray.CreateRange(additionalTexts))
                .RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generateDiagnostics);

            if (diagnostics == null)
            {
                Assert.False(
                    generateDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error || d.Severity == DiagnosticSeverity.Warning),
                    "Failed: " + generateDiagnostics.FirstOrDefault()?.GetMessage());
            }
            else
            {
                diagnostics.AddRange(generateDiagnostics);
            }

            string output = outputCompilation.SyntaxTrees.Last().ToString();
            Assert.NotNull(output);

            _output.WriteLine($"Generated code:\r\n===\r\n{output}\r\n===\r\n");

            return outputCompilation;
        }
    }
}