using System;
using System.Collections.Generic;
using JsonByExampleGenerator.Example.Json;
using Microsoft.Extensions.Configuration;

namespace JsonByExampleGenerator.Example
{
    /// <summary>
    /// This program just shows an example of how you can use the JsonByExampleGenerator.
    /// It's not required for testing, as there are unit tests in JsonByExampleGenerator.Tests.
    /// </summary>
    class Program
    {
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

            // Example of configuration
            IConfiguration config = new ConfigurationBuilder()
              .AddJsonFile("appsettings.json")
              .Build();

            Console.WriteLine($"Regular way using magic strings: {config.GetSection("Something").GetSection("SomeValue").Value}");
            Console.WriteLine($"Typed way: {Appsetting.FromConfig(config).Something.SomeValue}");
        }
    }
}
