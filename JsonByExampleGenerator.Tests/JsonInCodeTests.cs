using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace JsonByExampleGenerator.Tests
{
    public class JsonInCodeTests : TestsBase
    {
        public JsonInCodeTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void ShouldGenerate()
        {
            string source = @"using System;
using TestImplementation.Json;

namespace Example
{
    class Test
    {
        [JsonExample(""Example"")]
        private const string Json = ""{ \""prop\"" : \""val\"" }"";

        public static string RunTest()
        {
            var json = new TestImplementation.Json.Example.Example()
                {
                    Prop = ""propval""
                };
            return $""{json.Prop}"";
        }
    }
}";
            var compilation = GetGeneratedOutput(source, new Dictionary<string, string>());

            Assert.Equal("propval", RunTest(compilation));
        }
    }
}
