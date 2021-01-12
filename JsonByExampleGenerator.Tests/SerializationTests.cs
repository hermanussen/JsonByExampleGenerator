using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace JsonByExampleGenerator.Tests
{
    public class SerializationTests : TestsBase
    {
        public SerializationTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void ShouldSerializeAndDeserialize()
        {
            string jsonProduct = @"{
                            ""id"": 12,
                            ""name"": ""Example_product"",
                            ""region"": {
                                ""country"": ""Netherlands"",
                                ""provinces"": [ ""Limburg"", ""Friesland"" ],
                                ""enableAnalytics"": [ true, false ],
                                ""analyticsMultipliers"" : [ 1.1, 2.2 ]
                            },
                            ""colorVariants"": [
                              {
                                ""variantId"": 12,
                                ""color"": ""Red""
                              },
                              {
                                ""variantId"": 10,
                                ""color"": ""Green""
                              }
                            ]
                          }";

            string source = $@"using System;

namespace Example
{{
    class Test
    {{
        public static string RunTest()
        {{
            var product = System.Text.Json.JsonSerializer.Deserialize<TestImplementation.Json.Product>(@""{jsonProduct.Replace("\"", "\"\"")}"");
            
            var reserialized = System.Text.Json.JsonSerializer.Serialize(product);

            return $""{{reserialized}}"";
        }}
    }}
}}";

            var compilation = GetGeneratedOutput(source, new Dictionary<string, string>()
                {
                    {
                        "product.json",
                        jsonProduct
                    }
                });

            Assert.Equal(jsonProduct.Replace(" ", string.Empty).Replace("\r\n", string.Empty), RunTest(compilation));
        }
    }
}
