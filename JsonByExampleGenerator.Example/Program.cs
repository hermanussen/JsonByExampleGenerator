using System;
using System.Collections.Generic;
using JsonByExampleGenerator.Example.Json;
using JsonByExampleGenerator.Example.Json.Products;
using JsonByExampleGenerator.Example.Json.Animal;
using JsonByExampleGenerator.Example.Json.Appsettings;
using Microsoft.Extensions.Configuration;
using System.Runtime.Serialization;

namespace JsonByExampleGenerator.Example
{
    /// <summary>
    /// This program just shows an example of how you can use the JsonByExampleGenerator.
    /// It's not required for testing, as there are unit tests in JsonByExampleGenerator.Tests.
    /// </summary>
    class Program
    {
        // Example based on json that is defined in the code itself
        [JsonExample("Animal")]
        private const string AnimalJsonInCode = @"
{
  ""name"" : ""Spider"",
  ""legs"" : 8
}";

        static void Main()
        {
            // Example based on products.json
            var product = new Product()
                {
                    Id = 12,
                    Name = "Example product",
                    ColorVariants = new List<ColorVariant>()
                    {
                        new ColorVariant()
                        {
                            VariantId = 10,
                            Color = "Green"
                        },
                        new ColorVariant()
                        {
                            VariantId = 12,
                            Color = "Red"
                        }
                    }
                };

            Console.WriteLine($"id={product.Id}");
            Console.WriteLine($"name={product.Name}");

            // Example based on const string
            var spider = new Animal()
                {
                    Name = "Spider",
                    Legs = 8
                };

            Console.WriteLine($"name={spider.Name}");
            Console.WriteLine($"legs={spider.Legs}");

            // Example of configuration
            IConfiguration config = new ConfigurationBuilder()
              .AddJsonFile("appsettings.json")
              .Build();

            Console.WriteLine($"Regular way using magic strings: {config.GetSection("Something").GetSection("SomeValue").Value}");
            Console.WriteLine($"Typed way: {Appsetting.FromConfig(config).Something.SomeValue}");
        }
    }
}
