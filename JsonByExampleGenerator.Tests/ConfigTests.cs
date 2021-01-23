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
    /// Unit tests for using the generator to read configuration in a typed way.
    /// </summary>
    public class ConfigTests : TestsBase
    {
        public ConfigTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void ShouldGenerateConfig()
        {
            string source = @"using System;
using Microsoft.Extensions.Configuration;
using TestImplementation.Json;

namespace Example
{
    class Test
    {
        public static string RunTest()
        {
            IConfiguration config = new ConfigurationBuilder()
              .AddJsonFile(""appsettings.json"")
              .Build();

            return $""{Appsetting.FromConfig(config).AppSettings.ExampleVal}"";
        }
    }
}";
            var compilation = GetGeneratedOutput(source, new Dictionary<string, string>()
                {
                    { "example.json", "{ \"prop\" : \"val\" }" },
                    { "appsettings.json", "{ \"AppSettings\" : { \"exampleVal\" : \"Example value from settings\" } }" }
                });

            Assert.Equal("Example value from settings", RunTest(compilation));
        }
    }
}
