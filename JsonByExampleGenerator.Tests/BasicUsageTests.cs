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
            var json = new TestImplementation.Json.Example.Example()
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
            var json = new TestImplementation.Json.Example.Example()
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
            var json = new TestImplementation.Json.Example.Example()
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
            var json = new TestImplementation.Json.Example.Example()
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
            var json = new TestImplementation.Json.Example.Example()
                {
                    PropNull = new object(),
                    PropObject = new TestImplementation.Json.Example.PropObject()
                };
            return $""{json.PropNull} {json.PropObject}"";
        }
    }
}";
            var compilation = GetGeneratedOutput(source, new Dictionary<string, string>()
                {
                    { "example.json", "{ \"propNull\" : null, \"propObject\" : {} }" }
                });

            Assert.Equal("System.Object TestImplementation.Json.Example.PropObject", RunTest(compilation));
        }

        [Fact]
        public void ShouldGenerateArrayProperty()
        {
            string source = @"using System;
using System.Linq;
using System.Collections.Generic;

namespace Example
{
    class Test
    {
        public static string RunTest()
        {
            var json = new TestImplementation.Json.Example.Example();
            json.Violins ??= new List<TestImplementation.Json.Example.Violin>();
            json.Violins.Add(new TestImplementation.Json.Example.Violin()
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
using System.Collections.Generic;

namespace Example
{
    class Test
    {
        public static string RunTest()
        {
            var json = new TestImplementation.Json.Example.Example();
            json.Violins ??= new List<string>();
            json.Violins.Add(""My violin"");
            json.NumberSequence = new List<int>();
            json.NumberSequence.Add(33);
            json.BoolSequence = new List<bool>();
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
using System.Collections.Generic;

namespace Example
{
    class Test
    {
        public static string RunTest()
        {
            var json = new TestImplementation.Json.Example.Example();
            json.Violins ??= new List<TestImplementation.Json.Example.Violin>();
            json.Violins.Add(new TestImplementation.Json.Example.Violin()
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
            var json = new TestImplementation.Json.Violins.Violin()
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
            var json = new TestImplementation.Json.Example.Example()
                {
                    Violin = new TestImplementation.Json.Example.Violin()
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

        [Fact]
        public void ShouldGeneratePropertyNameDifferentFromClassName()
        {
            string source = @"using System;
using System.Linq;

namespace Example
{
    class Test
    {
        public static string RunTest()
        {
            var json = new TestImplementation.Json.Example.Example()
                {
                    Violins = new System.Collections.Generic.List<TestImplementation.Json.Example.Violin>()
                        {
                            new TestImplementation.Json.Example.Violin()
                            {
                                ViolinProperty = ""First""
                            }
                        }
                };
            return $""{json.Violins.First().ViolinProperty}"";
        }
    }
}";
            var compilation = GetGeneratedOutput(source, new Dictionary<string, string>()
                {
                    { "example.json", "{ \"violins\" : [{ \"violin\" : \"First\" } ] }" }
                });

            Assert.Equal("First", RunTest(compilation));
        }

        [Fact]
        public void ShouldGenerateDistinctPropertyName()
        {
            string source = @"using System;
using System.Linq;

namespace Example
{
    class Test
    {
        public static string RunTest()
        {
            var json = new TestImplementation.Json.Violin.Violin()
                {
                    ViolinProperty = ""First"",
                    ViolinProperty2 = ""Second"",
                    ViolinProperty3 = ""Third"",
                    ViolinProperty4 = ""Fourth""
                };
            return $""{json.ViolinProperty} {json.ViolinProperty2} {json.ViolinProperty3} {json.ViolinProperty4}"";
        }
    }
}";
            var compilation = GetGeneratedOutput(source, new Dictionary<string, string>()
                {
                    { "violin.json", "{ \"violin\" : \"First\", \"violinProperty\" : \"Second\", \"violinProperty2\" : \"Third\", \"violinProperty3\" : \"Fourth\" }" }
                });

            Assert.Equal("First Second Third Fourth", RunTest(compilation));
        }
    }
}
