using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace JsonByExampleGenerator.Tests
{
    /// <summary>
    /// Unit tests that check if you can properly override the default Scriban template in your code.
    /// </summary>
    public class TemplateOverrideTests : TestsBase
    {
        private const string json = "{ \"prop\" : \"val\" }";
        private const string workingTemplate = "{{ for ClassModel in ClassModels }}public class {{ ClassModel.ClassName }} { {{ for Property in ClassModel.Properties }} public {{ Property.PropertyType }} MyTempl{{ Property.PropertyName }} { get; set; } {{ end }} } {{ end }}";
        private const string brokenTemplate = "this will not compile as c#";

        public TemplateOverrideTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void ShouldGenerateWithCustomTemplate()
        {
            var additionalFilesAndContents = new Dictionary<string, string>()
                {
                    { Path.Combine("subfolder", "aa", "example2.json"), json },
                    { Path.Combine("subfolder", "aa", "example2.sbntxt"), workingTemplate }
                };

            GenerateWithCustomTemplate(additionalFilesAndContents);
        }

        [Fact]
        public void ShouldNotGenerateWithCustomTemplateInDifferentPath()
        {
            // In a different path, the template should not be picked up
            var additionalFilesAndContents = new Dictionary<string, string>()
                {
                    { Path.Combine("subfolder", "example2.json"), json },
                    { Path.Combine("subfolder2", "example2.sbntxt"), workingTemplate }
                };

            GenerateWithCustomTemplate(additionalFilesAndContents, true);
        }

        [Fact]
        public void ShouldNotGenerateWithBrokenTemplate()
        {
            // In a different path, the template should not be picked up
            var additionalFilesAndContents = new Dictionary<string, string>()
                {
                    { Path.Combine("subfolder", "example2.json"), json },
                    { Path.Combine("subfolder", "example2.sbntxt"), brokenTemplate }
                };

            GenerateWithCustomTemplate(additionalFilesAndContents, true);
        }

        [Fact]
        public void ShouldGenerateBasedOnDirectoryVersion()
        {
            var additionalFilesAndContents = new Dictionary<string, string>()
                {
                    { Path.Combine("subfolder", "example2.json"), json },
                    { Path.Combine("subfolder", "JsonByExampleTemplate.sbntxt"), workingTemplate }
                };

            GenerateWithCustomTemplate(additionalFilesAndContents);
        }

        [Fact]
        public void ShouldGenerateBasedOnAncestorDirectoryVersion()
        {
            var additionalFilesAndContents = new Dictionary<string, string>()
                {
                    { Path.Combine("bla", "example2.json"), json },
                    { "JsonByExampleTemplate.sbntxt", workingTemplate }
                };

            GenerateWithCustomTemplate(additionalFilesAndContents);
        }

        [Fact]
        public void ShouldGenerateBasedOnParentDirectoryVersion()
        {
            var additionalFilesAndContents = new Dictionary<string, string>()
                {
                    { Path.Combine("subfolder", "subsubfolder", "example2.json"), json },
                    { Path.Combine("JsonByExampleTemplate.sbntxt"), brokenTemplate },
                    { Path.Combine("subfolder", "JsonByExampleTemplate.sbntxt"), workingTemplate },
                };

            GenerateWithCustomTemplate(additionalFilesAndContents);
        }



        [Fact]
        public void ShouldGenerateBasedOnOnlyOnceTemplate()
        {
            string source = @"using System;

namespace Example
{
    class Test
    {
        public static string RunTest()
        {
            return $""{TestImplementation.Json.Something.GetMessage()}"";
        }
    }
}";

            var additionalFilesAndContents = new Dictionary<string, string>()
                {
                    { Path.Combine("OnlyOnceTemplate.sbntxt"), @"namespace {{ NamespaceName }}.Json { public class Something { public static string GetMessage() { return ""somemessage""; }  } }" }
                };

            var compilation = GetGeneratedOutput(source, additionalFilesAndContents);
            var testOutput = RunTest(compilation);

            Assert.Equal("somemessage", testOutput);
        }

        private void GenerateWithCustomTemplate(Dictionary<string, string> additionalFilesAndContents, bool shouldFail = false)
        {
            string source = @"using System;

namespace Example
{
    class Test
    {
        public static string RunTest()
        {
            var json = new Example2()
                {
                    MyTemplProp = ""propval""
                };
            return $""{json.MyTemplProp}"";
        }
    }
}";
            var diagnostics = new List<Diagnostic>();

            var compilation = GetGeneratedOutput(source, additionalFilesAndContents, diagnostics);
            var testOutput = RunTest(compilation, diagnostics);

            if (shouldFail)
            {
                Assert.True(diagnostics.Any());
                return;
            }
            
            Assert.Equal("propval", testOutput);
        }
    }
}
