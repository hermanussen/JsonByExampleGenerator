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
    /// Tests for difference in behavior when placing json files in the same or different folders.
    /// </summary>
    public class FolderDependentTests : TestsBase
    {
        public FolderDependentTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void ShouldGenerateInSameFolder()
        {
            string source = @"using System;

namespace Example
{
    class Test
    {
        public static string RunTest()
        {
            var json = new TestImplementation.Json.Product.Product()
                {
                    Id = 1,
                    Name = ""Product one""
                };
            var json2 = new TestImplementation.Json.Products.Product()
                {
                    Id = 2
                };
            return $""{json.Name} {json2.Id}"";
        }
    }
}";
            var compilation = GetGeneratedOutput(source, new Dictionary<string, string>()
                {
                    { "product.json", "{ \"id\" : 1, \"name\" : \"Product one\" }" },
                    { "products.json", "[{ \"id\" : 1 }, { \"id\" : 2 }, { \"id\" : 3 }]" }
                });

            Assert.Equal("Product one 2", RunTest(compilation));
        }
    }
}
