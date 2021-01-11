using System;
using System.Collections.Generic;
using System.Diagnostics;
using JsonByExampleGenerator.Example.Json;

namespace JsonByExampleGenerator.Example
{
    class Program
    {
        static void Main()
        {
            var product = new Product()
                {
                    Id = 12,
                    Name = "Example product"
                };

            product.ColorVariants.Add(new ColorVariant()
                {
                    VariantId = 12,
                    Color = "Red"
                });

            product.ColorVariants.Add(new ColorVariant()
                {
                    VariantId = 10,
                    Color = "Green"
                });

            Console.WriteLine($"id={product.Id}");
            Console.WriteLine($"name={product.Name}");
        }
    }
}
