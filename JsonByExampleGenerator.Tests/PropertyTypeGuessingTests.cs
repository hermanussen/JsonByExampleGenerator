using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace JsonByExampleGenerator.Tests
{
    public class PropertyTypeGuessingTests : TestsBase
    {
        public PropertyTypeGuessingTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void ShouldGenerateNumericDouble()
        {
            var jsonElements = new[]
                {
                    "1",
                    "1.1",
                    "1"
                };

            var compilation = GetGeneratedOutputSingleProperty(jsonElements);

            Assert.Equal("Double", RunTest(compilation));
        }

        [Fact]
        public void ShouldGenerateNumericDecimal()
        {
            var jsonElements = new[]
                {
                    "1.2",
                    "1.14485738948858773743888",
                    "1.3"
                };

            var compilation = GetGeneratedOutputSingleProperty(jsonElements);

            Assert.Equal("Decimal", RunTest(compilation));
        }

        [Fact]
        public void ShouldGenerateNumericInt()
        {
            var jsonElements = new[]
                {
                    "2782",
                    "7488833"
                };

            var compilation = GetGeneratedOutputSingleProperty(jsonElements);

            Assert.Equal("Int32", RunTest(compilation));
        }

        [Fact]
        public void ShouldGenerateNumericLong()
        {
            var jsonElements = new[]
                {
                    "-12",
                    (Convert.ToInt64(int.MaxValue) + 1).ToString()
                };

            var compilation = GetGeneratedOutputSingleProperty(jsonElements);

            Assert.Equal("Int64", RunTest(compilation));
        }

        [Fact]
        public void ShouldGenerateDateTime()
        {
            var localDateTime = new DateTime(2021, 1, 25, 13, 19, 33, 987, DateTimeKind.Local);
            var utcDateTime = new DateTime(2021, 1, 25, 13, 19, 33, DateTimeKind.Utc);
            var unspecifiedDateTime = new DateTime(2021, 1, 25, 13, 19, 33, 987, DateTimeKind.Unspecified);
            var jsonElements = new[]
                {
                    JsonSerializer.Serialize(localDateTime),
                    JsonSerializer.Serialize(utcDateTime),
                    JsonSerializer.Serialize(unspecifiedDateTime),
                    $"{JsonSerializer.Serialize(utcDateTime).TrimEnd('\"', 'Z')}+02:00\"",
                    $"{JsonSerializer.Serialize(utcDateTime).TrimEnd('\"', 'Z')}-07:00\"",
               };

            var compilation = GetGeneratedOutputSingleProperty(jsonElements);

            Assert.Equal("DateTime", RunTest(compilation));
        }

        [Fact]
        public void ShouldNotGenerateDateTime()
        {
            var localDateTime = new DateTime(2021, 1, 25, 13, 19, 33, 987, DateTimeKind.Local);
            var jsonElements = new[]
                {
                    JsonSerializer.Serialize(localDateTime),
                    "\"Not a date time\""
               };

            var compilation = GetGeneratedOutputSingleProperty(jsonElements);

            Assert.Equal("String", RunTest(compilation));
        }

        private Compilation GetGeneratedOutputSingleProperty(object[] jsonElements)
        {
            const string singlePropertyGetTypeSrc = @"using System;
using System.Linq;
using System.Reflection;

namespace Example
{
    class Test
    {
        public static string RunTest()
        {
            var propertyType = typeof(TestImplementation.Json.Example).GetProperties().First().PropertyType.Name;
            return $""{propertyType}"";
        }
    }
}";

            return GetGeneratedOutput(singlePropertyGetTypeSrc, new Dictionary<string, string>()
                {
                    { "examples.json", $"[{ string.Join(", ", jsonElements.Select(e => $"{{ \"prop\" : {e} }}")) }]" }
                });
        }
    }
}
