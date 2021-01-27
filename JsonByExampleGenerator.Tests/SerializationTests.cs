using FluentAssertions;
using JsonByExampleGenerator.Generator.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace JsonByExampleGenerator.Tests
{
    /// <summary>
    /// Unit tests that check if the generated code correctly serializes and deserializes to and from json.
    /// </summary>
    public class SerializationTests : TestsBase
    {
        public SerializationTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void ShouldSerializeAndDeserialize()
        {
            const string rootTypeName = "Product";
            string jsonAsString = @"{
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

            DeserializeSerializeAndAssert(rootTypeName, jsonAsString, "product.json");
        }

        [Theory]
        [InlineData("JsonOrgExample1", "RealWorldExamples/jsonOrgExample1.json")]
        [InlineData("JsonOrgExample2", "RealWorldExamples/jsonOrgExample2.json")]
        [InlineData("JsonOrgExample3", "RealWorldExamples/jsonOrgExample3.json")]
        [InlineData("JsonOrgExample4", "RealWorldExamples/jsonOrgExample4.json")]
        [InlineData("JsonOrgExample5", "RealWorldExamples/jsonOrgExample5.json")]
        [InlineData("SitepointColorsExample", "RealWorldExamples/sitepointColorsExample.json")]
        [InlineData("SitepointGoogleMapsExample", "RealWorldExamples/sitepointGoogleMapsExample.json")]
        [InlineData("SitepointYoutubeExample", "RealWorldExamples/sitepointYoutubeExample.json")]
        public void ShouldSerializeAndDeserializeFromFile(string rootTypeName, string jsonFilePath)
        {
            DeserializeSerializeAndAssert(
                rootTypeName,
                EmbeddedResource.GetContent(jsonFilePath, System.Reflection.Assembly.GetExecutingAssembly()),
                Path.GetFileName(jsonFilePath));
        }

        private void DeserializeSerializeAndAssert(string rootTypeName, string jsonAsString, string fileName)
        {
            string source = $@"using System;
using System.IO;
using System.Runtime.Serialization.Json;

namespace Example
{{
    class Test
    {{
        public static string RunTest()
        {{
            var readStream = new MemoryStream();
            var streamWriter = new StreamWriter(readStream);
            streamWriter.Write(@""{jsonAsString.Replace("\"", "\"\"")}"");
            streamWriter.Flush();
            readStream.Position = 0;
    
            var ser = new DataContractJsonSerializer(typeof(TestImplementation.Json.{rootTypeName}.{rootTypeName}));
            var rootType = (TestImplementation.Json.{rootTypeName}.{rootTypeName}) ser.ReadObject(readStream);
            
            var writeStream = new MemoryStream();
            ser.WriteObject(writeStream, rootType);
            writeStream.Position = 0;
            var streamReader = new StreamReader(writeStream);

            return $""{{streamReader.ReadToEnd()}}"".Replace(""\\/"", ""/"");
        }}
    }}
}}";
            
            var compilation = GetGeneratedOutput(source, new Dictionary<string, string>()
                {
                    {
                        fileName,
                        jsonAsString
                    }
                });

            string? actualAsString = RunTest(compilation);

            JToken expected = JToken.Parse(jsonAsString);
            JToken actual = JToken.Parse(actualAsString);

            _output.WriteLine($"Expected: {jsonAsString}");
            _output.WriteLine($"Actual: {actualAsString}");

            actual.Should().BeEquivalentTo(expected);
        }
    }
}
