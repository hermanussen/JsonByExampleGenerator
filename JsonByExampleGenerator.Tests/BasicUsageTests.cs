using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace JsonByExampleGenerator.Tests
{
    /// <summary>
    /// Unit tests basic usage scenarios.
    /// </summary>
    public class BasicUsageTests : TestsBase
    {
        public BasicUsageTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void ShouldGenerate()
        {
            string source = @"using System;

namespace Example
{
    class Test
    {
        public static string RunTest()
        {
            var json = new TestImplementation.Json.Example()
                {
                    Prop = ""propval""
                };
            return $""{json.Prop}"";
        }
    }
}";
            var compilation = GetGeneratedOutput(source, new Dictionary<string, string>()
                {
                    { "example.json", "{ \"prop\" : \"val\" }" }
                });

            Assert.Equal("propval", RunTest(compilation));
        }

        [Fact]
        public void ShouldGenerateWithSpaces()
        {
            string source = @"using System;

namespace Example
{
    class Test
    {
        public static string RunTest()
        {
            var json = new TestImplementation.Json.Example()
                {
                    PropSpace = ""propval""
                };
            return $""{json.PropSpace}"";
        }
    }
}";
            var compilation = GetGeneratedOutput(source, new Dictionary<string, string>()
                {
                    { "example.json", "{ \"prop space\" : \"val\" }" }
                });

            Assert.Equal("propval", RunTest(compilation));
        }

        [Fact]
        public void ShouldGenerateNumericProperty()
        {
            string source = @"using System;

namespace Example
{
    class Test
    {
        public static string RunTest()
        {
            var json = new TestImplementation.Json.Example()
                {
                    PropNum = 800000,
                    PropNumDecimals = 3.14
                };
            return $""{json.PropNum} {json.PropNumDecimals.ToString(new System.Globalization.CultureInfo(""en""))}"";
        }
    }
}";
            var compilation = GetGeneratedOutput(source, new Dictionary<string, string>()
                {
                    { "example.json", "{ \"propNum\" : 2, \"propNumDecimals\" : 3.14 }" }
                });

            Assert.Equal("800000 3.14", RunTest(compilation));
        }

        [Fact]
        public void ShouldGenerateBoolProperty()
        {
            string source = @"using System;

namespace Example
{
    class Test
    {
        public static string RunTest()
        {
            var json = new TestImplementation.Json.Example()
                {
                    PropTrue = true,
                    PropFalse = false
                };
            return $""{json.PropTrue} {json.PropFalse}"";
        }
    }
}";
            var compilation = GetGeneratedOutput(source, new Dictionary<string, string>()
                {
                    { "example.json", "{ \"propTrue\" : true, \"propFalse\" : false }" }
                });

            Assert.Equal("True False", RunTest(compilation));
        }

        [Fact]
        public void ShouldGenerateObjectProperty()
        {
            string source = @"using System;

namespace Example
{
    class Test
    {
        public static string RunTest()
        {
            var json = new TestImplementation.Json.Example()
                {
                    PropNull = new object(),
                    PropObject = new TestImplementation.Json.PropObject()
                };
            return $""{json.PropNull} {json.PropObject}"";
        }
    }
}";
            var compilation = GetGeneratedOutput(source, new Dictionary<string, string>()
                {
                    { "example.json", "{ \"propNull\" : null, \"propObject\" : {} }" }
                });

            Assert.Equal("System.Object TestImplementation.Json.PropObject", RunTest(compilation));
        }

        [Fact]
        public void ShouldGenerateArrayProperty()
        {
            string source = @"using System;
using System.Linq;

namespace Example
{
    class Test
    {
        public static string RunTest()
        {
            var json = new TestImplementation.Json.Example();
            json.Violins.Add(new TestImplementation.Json.Violin()
                                {
                                    Name = ""My violin""
                                });
            return $""{json.Violins.First().Name}"";
        }
    }
}";
            var compilation = GetGeneratedOutput(source, new Dictionary<string, string>()
                {
                    { "example.json", "{ \"violins\" : [{ \"name\" : \"First\" }, { \"name\" : \"Second\" }] }" }
                });

            Assert.Equal("My violin", RunTest(compilation));
        }

        [Fact]
        public void ShouldGenerateArrayPropertyStringsAndNumbersAndBools()
        {
            string source = @"using System;
using System.Linq;

namespace Example
{
    class Test
    {
        public static string RunTest()
        {
            var json = new TestImplementation.Json.Example();
            json.Violins.Add(""My violin"");
            json.NumberSequence.Add(33);
            json.BoolSequence.Add(true);
            return $""{json.Violins.First()} {json.NumberSequence.First()} {json.BoolSequence.First()}"";
        }
    }
}";
            var compilation = GetGeneratedOutput(source, new Dictionary<string, string>()
                {
                    { "example.json", "{ \"violins\" : [ \"One\", \"Two\" ], \"numberSequence\" : [ 22, 44 ], \"boolSequence\" : [ true, false, true ] }" }
                });

            Assert.Equal("My violin 33 True", RunTest(compilation));
        }

        [Fact]
        public void ShouldGenerateAndExtendArrayProperty()
        {
            string source = @"using System;
using System.Linq;

namespace Example
{
    class Test
    {
        public static string RunTest()
        {
            var json = new TestImplementation.Json.Example();
            json.Violins.Add(new TestImplementation.Json.Violin()
                                {
                                    Name = ""My violin"",
                                    AnotherProperty = 1337
                                });
            return $""{json.Violins.First().AnotherProperty}"";
        }
    }
}";
            var compilation = GetGeneratedOutput(source, new Dictionary<string, string>()
                {
                    { "example.json", "{ \"violins\" : [{ \"name\" : \"First\" }, { \"name\" : \"Second\", \"anotherProperty\" : 1337 }] }" }
                });

            Assert.Equal("1337", RunTest(compilation));
        }

        [Fact]
        public void ShouldGenerateFromTopLevelArray()
        {
            string source = @"using System;
using System.Linq;

namespace Example
{
    class Test
    {
        public static string RunTest()
        {
            var json = new TestImplementation.Json.Violin()
                {
                    Name = ""My violin"",
                    AnotherProperty = 1337
                };
            return $""{json.AnotherProperty}"";
        }
    }
}";
            var compilation = GetGeneratedOutput(source, new Dictionary<string, string>()
                {
                    { "violins.json", "[{ \"name\" : \"First\" }, { \"name\" : \"Second\", \"anotherProperty\" : 1337 }]" }
                });

            Assert.Equal("1337", RunTest(compilation));
        }

        [Fact]
        public void ShouldGenerateTypedObject()
        {
            string source = @"using System;
using System.Linq;

namespace Example
{
    class Test
    {
        public static string RunTest()
        {
            var json = new TestImplementation.Json.Example()
                {
                    Violin = new TestImplementation.Json.Violin()
                        {
                            Name = ""First""
                        }
                };
            return $""{json.Violin.Name}"";
        }
    }
}";
            var compilation = GetGeneratedOutput(source, new Dictionary<string, string>()
                {
                    { "example.json", "{ \"violin\" : { \"name\" : \"First\" } }" }
                });

            Assert.Equal("First", RunTest(compilation));
        }
    }
}
