using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace JsonByExampleGenerator.Tests
{
    public class ChangesToGeneratedCodeTests : TestsBase
    {
        public ChangesToGeneratedCodeTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void ShouldGenerateWithChangedPropertyTypeOrName()
        {
            string source = @"using System;
using System.Text.Json.Serialization;

namespace TestImplementation.Json
{
    public partial class Example
    {
        // Change the type to int, instead of string
        [JsonPropertyName(""prop"")]
        public int Prop { get; set; }

        // Change the name of the property, but reference json property properly
        [JsonPropertyName(""another"")]
        public int YetAnother { get; set; }
    }
}

namespace Example
{
    class Test
    {
        public static string RunTest()
        {
            var json = new TestImplementation.Json.Example()
                {
                    Prop = 111,
                    YetAnother = 33
                };
            return $""{json.Prop} {json.YetAnother}"";
        }
    }
}";
            var compilation = GetGeneratedOutput(source, new Dictionary<string, string>()
                {
                    { "example.json", "{ \"prop\" : \"val\", \"another\" : 22 }" }
                });

            Assert.Equal("111 33", RunTest(compilation));
        }

        [Fact]
        public void ShouldGenerateWithChangedClassName()
        {
            string source = @"using System;
using System.Text.Json.Serialization;

namespace TestImplementation.Json
{
    [JsonRenamedFrom(""Example"")]
    public partial class DifferentName
    {
    }
}

namespace Example
{
    class Test
    {
        public static string RunTest()
        {
            var json = new TestImplementation.Json.DifferentName()
                {
                    Prop = ""testval""
                };
            return $""{json.Prop}"";
        }
    }
}";
            var compilation = GetGeneratedOutput(source, new Dictionary<string, string>()
                {
                    { "example.json", "{ \"prop\" : \"val\" }" }
                });

            Assert.Equal("testval", RunTest(compilation));
        }

        [Fact]
        public void ShouldGenerateWithChangedClassNameUsedInProperties()
        {
            string source = @"using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TestImplementation.Json
{
    [JsonRenamedFrom(""WeirdProduct"")]
    public partial class Product
    {
    }

    [JsonRenamedFrom(""WeirdAnimal"")]
    public partial class Animal
    {
    }
}

namespace Example
{
    class Test
    {
        public static string RunTest()
        {
            var json = new TestImplementation.Json.Example()
                {
                    Prop = ""testval"",
                    WeirdProduct = new TestImplementation.Json.Product()
                        {
                            ProductId = 2,
                            Name = ""Example product 2""
                        },
                    WeirdAnimals = new List<TestImplementation.Json.Animal>()
                        {
                            new TestImplementation.Json.Animal()
                                {
                                    Name = ""Octopus""
                                }
                        }
                };
            return $""{json.Prop} {json.WeirdProduct.ProductId} {json.WeirdAnimals.First().Name}"";
        }
    }
}";
            var compilation = GetGeneratedOutput(source, new Dictionary<string, string>()
                {
                    { "example.json", "{ \"prop\" : \"val\", \"weirdProduct\" : { \"ProductId\" : 1, \"Name\": \"Example product\" }, \"weirdAnimals\" : [{ \"Name\": \"Stingray\" }] }" }
                });

            Assert.Equal("testval 2 Octopus", RunTest(compilation));
        }
    }
}
