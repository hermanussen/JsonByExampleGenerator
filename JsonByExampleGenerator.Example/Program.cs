using System;
using System.Diagnostics;

namespace JsonByExampleGenerator.Example
{
    class Program
    {
        static void Main(string[] args)
        {
            // In the following line, "ExampleJson" could be anything, as long as the name ends with Json.
            // The source generator will generate a class for it and will use the referenced json file to generate the property names.
            var f = new ExampleJson("example.json")
                {
                    Name = "Name set in example program",
                    Amount = 10
                };
            
            Console.WriteLine($"I really just invented the type on the spot, and now I can use it...");
            Console.WriteLine($"name={f.Name}");
            Console.WriteLine($"amount={f.Amount}");
        }
    }
}
