using System;
using System.Diagnostics;

namespace JsonByExampleGenerator.Example
{
    class Program
    {
        static void Main(string[] args)
        {
            typeof(Fictional).RegisterByExample("example.json");

            var f = new Fictional()
                {
                    Name = "Name set in example program",
                    Amount = 10
                };
            
            Console.WriteLine($"I really just invented the type on the spot, and now I can use it, like...\r\nname={f.Name}\r\namount={f.Amount}");
        }
    }
}
