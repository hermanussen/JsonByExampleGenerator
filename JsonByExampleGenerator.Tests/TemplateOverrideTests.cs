using System;
using System.Collections.Generic;
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
        public TemplateOverrideTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void ShouldGenerateWithCustomTemplate()
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
            var compilation = GetGeneratedOutput(source, new Dictionary<string, string>()
                {
                    { "example2.json", "{ \"prop\" : \"val\" }" },
                    { "example2.sbntxt", "{{ for ClassModel in ClassModels }}public class {{ ClassModel.ClassName }} { {{ for Property in ClassModel.Properties }} public {{ Property.PropertyType }} MyTempl{{ Property.PropertyName }} { get; set; } {{ end }} } {{ end }}" }
                });

            Assert.Equal("propval", RunTest(compilation));
        }
    }
}
