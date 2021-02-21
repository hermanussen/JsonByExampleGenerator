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
    /// Test scenario's where names would be invalid in C#.
    /// </summary>
    public class ConflictingNamesTests : TestsBase
    {
        public ConflictingNamesTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void ShouldGenerateSystem()
        {
            string source = @"using System;

namespace Example
{
    class Test
    {
        public static string RunTest()
        {
            var json = new TestImplementation.Json.SystemX.SystemX()
                {
                    Prop = ""propval""
                };
            return $""{json.Prop}"";
        }
    }
}";
            var compilation = GetGeneratedOutput(source, new Dictionary<string, string>()
            {
                { "system.json", "{ \"prop\" : \"val\" }" }
            });

            Assert.Equal("propval", RunTest(compilation));
        }

        [Fact]
        public void ShouldGenerateWithDotInPropName()
        {
            string source = @"using System;

namespace Example
{
    class Test
    {
        public static string RunTest()
        {
            var json = new TestImplementation.Json.Ex.Ex()
                {
                    PropBla = ""propval""
                };
            return $""{json.PropBla}"";
        }
    }
}";
            var compilation = GetGeneratedOutput(source, new Dictionary<string, string>()
            {
                { "ex.json", "{ \"prop.bla\" : \"val\" }" }
            });

            Assert.Equal("propval", RunTest(compilation));
        }
    }
}
