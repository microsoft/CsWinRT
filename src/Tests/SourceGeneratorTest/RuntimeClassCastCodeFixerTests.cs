// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CSharpCodeFixTest = SourceGeneratorTest.Helpers.CSharpCodeFixTest<
    Generator.RuntimeClassCastAnalyzer,
    Generator.RuntimeClassCastCodeFixer>;

namespace SourceGeneratorTest;

[TestClass]
public class RuntimeClassCastCodeFixerTests
{
    [TestMethod]
    public async Task SingleCast_Method()
    {
        const string original = """
            using WinRT;

            namespace MyApp;

            public class Program
            {
                public void M(object obj)
                {
                    C c = (C)obj;
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;
            """;

        const string @fixed = """
            using WinRT;

            namespace MyApp;

            public class Program
            {
                [DynamicWindowsRuntimeCast(typeof(C))]
                public void M(object obj)
                {
                    C c = (C)obj;
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;
            """;

        CSharpCodeFixTest test = new(LanguageVersion.Preview)
        {
            TestCode = original,
            FixedCode = @fixed
        };

        await test.RunAsync();
    }

    [TestMethod]
    public async Task MultipleCasts_Method()
    {
        const string original = """
            using WinRT;

            namespace MyApp;

            public class Program
            {
                public void M(object obj)
                {
                    C c = (C)obj;
                    D d = (D)obj;
                    E e = (E)obj;
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;

            [WindowsRuntimeType("SomeContract")]
            class D;

            [WindowsRuntimeType("SomeContract")]
            enum E
            {
                A,
                B
            }
            """;

        const string @fixed = """
            using WinRT;

            namespace MyApp;

            public class Program
            {
                [DynamicWindowsRuntimeCast(typeof(C))]
                [DynamicWindowsRuntimeCast(typeof(D))]
                [DynamicWindowsRuntimeCast(typeof(E))]
                public void M(object obj)
                {
                    C c = (C)obj;
                    D d = (D)obj;
                    E e = (E)obj;
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;

            [WindowsRuntimeType("SomeContract")]
            class D;

            [WindowsRuntimeType("SomeContract")]
            enum E
            {
                A,
                B
            }
            """;

        CSharpCodeFixTest test = new(LanguageVersion.Preview)
        {
            TestCode = original,
            FixedCode = @fixed
        };

        await test.RunAsync();
    }

    [TestMethod]
    public async Task MultipleCasts_Method_WithTriviaAndLeadingAttributes()
    {
        const string original = """
            using WinRT;

            namespace MyApp;

            public class Program
            {
                /// <summary>
                /// Blah.
                /// </summary>
                [Dummy]
                public void M(object obj)
                {
                    C c = (C)obj;
                    D d = (D)obj;
                    E e = (E)obj;
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;

            [WindowsRuntimeType("SomeContract")]
            class D;

            [WindowsRuntimeType("SomeContract")]
            enum E
            {
                A,
                B
            }

            public class DummyAttribute : Attribute;
            """;

        const string @fixed = """
            using WinRT;

            namespace MyApp;

            public class Program
            {
                /// <summary>
                /// Blah.
                /// </summary>
                [Dummy]
                [DynamicWindowsRuntimeCast(typeof(C))]
                [DynamicWindowsRuntimeCast(typeof(D))]
                [DynamicWindowsRuntimeCast(typeof(E))]
                public void M(object obj)
                {
                    C c = (C)obj;
                    D d = (D)obj;
                    E e = (E)obj;
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;

            [WindowsRuntimeType("SomeContract")]
            class D;

            [WindowsRuntimeType("SomeContract")]
            enum E
            {
                A,
                B
            }

            public class DummyAttribute : Attribute;
            """;

        CSharpCodeFixTest test = new(LanguageVersion.Preview)
        {
            TestCode = original,
            FixedCode = @fixed
        };

        await test.RunAsync();
    }

    [TestMethod]
    public async Task SingleCast_LocalMethod()
    {
        const string original = """
            using WinRT;

            namespace MyApp;

            public class Program
            {
                public void M(object obj)
                {
                    void N(object obj)
                    {
                        C c = (C)obj;
                    }

                    N(obj);
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;
            """;

        const string @fixed = """
            using WinRT;

            namespace MyApp;

            public class Program
            {
                [DynamicWindowsRuntimeCast(typeof(C))]
                public void M(object obj)
                {
                    void N(object obj)
                    {
                        C c = (C)obj;
                    }

                    N(obj);
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;
            """;

        CSharpCodeFixTest test = new(LanguageVersion.Preview)
        {
            TestCode = original,
            FixedCode = @fixed
        };

        await test.RunAsync();
    }

    [TestMethod]
    public async Task SingleCast_FieldInitializer()
    {
        const string original = """
            using System;
            using WinRT;

            namespace MyApp;

            public class Program
            {
                private static readonly object obj = Register(obj => (C)obj);

                private static object Register(Action<object> action)
                {
                    return new();
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;
            """;

        const string @fixed = """
            using System;
            using WinRT;

            namespace MyApp;

            public class Program
            {
                [DynamicWindowsRuntimeCast(typeof(C))]
                private static readonly object obj = Register(obj => (C)obj);

                private static object Register(Action<object> action)
                {
                    return new();
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;
            """;

        CSharpCodeFixTest test = new(LanguageVersion.Preview)
        {
            TestCode = original,
            FixedCode = @fixed
        };

        await test.RunAsync();
    }
}
