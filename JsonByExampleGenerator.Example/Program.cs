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
        }
    }
}
