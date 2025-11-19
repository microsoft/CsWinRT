// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CSharpCodeFixTest = SourceGeneratorTest.Helpers.CSharpCodeFixTest<
    Generator.RuntimeClassCastAnalyzer,
    Generator.RuntimeClassCastCodeFixer>;

namespace SourceGeneratorTest;

// [TestClass]
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
                    C c = {|CsWinRT1034:(C)obj|};
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

        CSharpCodeFixTest test = new(LanguageVersion.CSharp13, editorconfig: [("CsWinRTAotWarningLevel", "3")])
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
                    C c = {|CsWinRT1034:(C)obj|};
                    D d = {|CsWinRT1034:(D)obj|};
                    E e = {|CsWinRT1035:(E)obj|};
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

        CSharpCodeFixTest test = new(LanguageVersion.CSharp13, editorconfig: [("CsWinRTAotWarningLevel", "3")])
        {
            TestCode = original,
            FixedCode = @fixed
        };

        await test.RunAsync();
    }

    [TestMethod]
    public async Task MultipleCasts_Method_WithDuplicateTypes()
    {
        const string original = """
            using WinRT;

            namespace MyApp;

            public class Program
            {
                public void M(object obj)
                {
                    C c1 = {|CsWinRT1034:(C)obj|};
                    C c2 = {|CsWinRT1034:(C)obj|};
                    D d = {|CsWinRT1034:(D)obj|};
                    E e1 = {|CsWinRT1035:(E)obj|};
                    E e2 = {|CsWinRT1035:(E)obj|};
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
                    C c1 = (C)obj;
                    C c2 = (C)obj;
                    D d = (D)obj;
                    E e1 = (E)obj;
                    E e2 = (E)obj;
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

        CSharpCodeFixTest test = new(LanguageVersion.CSharp13, editorconfig: [("CsWinRTAotWarningLevel", "3")])
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
            using System;
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
                    C c = {|CsWinRT1034:(C)obj|};
                    D d = {|CsWinRT1034:(D)obj|};
                    E e = {|CsWinRT1035:(E)obj|};
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
            using System;
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

        CSharpCodeFixTest test = new(LanguageVersion.CSharp13, editorconfig: [("CsWinRTAotWarningLevel", "3")])
        {
            TestCode = original,
            FixedCode = @fixed
        };

        await test.RunAsync();
    }

    [TestMethod]
    public async Task SingleCast_Constructor()
    {
        const string original = """
            using WinRT;

            namespace MyApp;

            public class Program
            {
                public Program(object obj)
                {
                    C c = {|CsWinRT1034:(C)obj|};
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
                public Program(object obj)
                {
                    C c = (C)obj;
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;
            """;

        CSharpCodeFixTest test = new(LanguageVersion.CSharp13, editorconfig: [("CsWinRTAotWarningLevel", "3")])
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
                        C c = {|CsWinRT1034:(C)obj|};
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

        CSharpCodeFixTest test = new(LanguageVersion.CSharp13, editorconfig: [("CsWinRTAotWarningLevel", "3")])
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
                private static readonly object obj = Register(obj => {|CsWinRT1034:(C)obj|});

                private static object Register(Func<object, C> action)
                {
                    return action(new object());
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

                private static object Register(Func<object, C> action)
                {
                    return action(new object());
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;
            """;

        CSharpCodeFixTest test = new(LanguageVersion.CSharp13, editorconfig: [("CsWinRTAotWarningLevel", "3")])
        {
            TestCode = original,
            FixedCode = @fixed
        };

        await test.RunAsync();
    }

    [TestMethod]
    public async Task SingleCast_Property_OneAccessor()
    {
        const string original = """
            using WinRT;

            namespace MyApp;

            public class Program
            {
                object _obj;

                C P1
                {
                    get => {|CsWinRT1034:(C)_obj|};
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
                object _obj;

                C P1
                {
                    [DynamicWindowsRuntimeCast(typeof(C))]
                    get => (C)_obj;
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;
            """;

        CSharpCodeFixTest test = new(LanguageVersion.CSharp13, editorconfig: [("CsWinRTAotWarningLevel", "3")])
        {
            TestCode = original,
            FixedCode = @fixed
        };

        await test.RunAsync();
    }

    [TestMethod]
    public async Task SingleCast_Property_TwoAccessor_OnlyOneWarns()
    {
        const string original = """
            using WinRT;

            namespace MyApp;

            public class Program
            {
                object _obj;

                C P1
                {
                    get => {|CsWinRT1034:(C)_obj|};
                    set => _obj = value;
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
                object _obj;

                C P1
                {
                    [DynamicWindowsRuntimeCast(typeof(C))]
                    get => (C)_obj;
                    set => _obj = value;
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;
            """;

        CSharpCodeFixTest test = new(LanguageVersion.CSharp13, editorconfig: [("CsWinRTAotWarningLevel", "3")])
        {
            TestCode = original,
            FixedCode = @fixed
        };

        await test.RunAsync();
    }

    [TestMethod]
    public async Task SingleCast_Property_TwoAccessor_BothWarn()
    {
        const string original = """
            using WinRT;

            namespace MyApp;

            public class Program
            {
                object _obj;
                D _d;

                C P1
                {
                    get => {|CsWinRT1034:(C)_obj|};
                    set => _d = {|CsWinRT1034:(D)value|};
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;

            [WindowsRuntimeType("SomeContract")]
            class D : C;
            """;

        const string @fixed = """
            using WinRT;

            namespace MyApp;

            public class Program
            {
                object _obj;
                D _d;

                C P1
                {
                    [DynamicWindowsRuntimeCast(typeof(C))]
                    get => (C)_obj;
                    [DynamicWindowsRuntimeCast(typeof(D))]
                    set => _d = (D)value;
                }
            }

            [WindowsRuntimeType("SomeContract")]
            class C;

            [WindowsRuntimeType("SomeContract")]
            class D : C;
            """;

        CSharpCodeFixTest test = new(LanguageVersion.CSharp13, editorconfig: [("CsWinRTAotWarningLevel", "3")])
        {
            TestCode = original,
            FixedCode = @fixed
        };

        await test.RunAsync();
    }

    [TestMethod]
    public async Task MultipleCasts_MixedMembers_WithDuplicateTypes()
    {
        const string original = """
            using WinRT;

            namespace MyApp;

            public class Program
            {
                private object _obj;

                public void M(object obj)
                {
                    C c1 = {|CsWinRT1034:(C)obj|};
                    C c2 = {|CsWinRT1034:(C)obj|};
                    D d = {|CsWinRT1034:(D)obj|};
                    E e1 = {|CsWinRT1035:(E)obj|};
                    E e2 = {|CsWinRT1035:(E)obj|};
                    F f1 = {|CsWinRT1034:(F)obj|};
                    F f2 = {|CsWinRT1034:(F)obj|};
                    E e3 = {|CsWinRT1035:(E)obj|};
                }

                C P1
                {
                    get => {|CsWinRT1034:(C)_obj|};
                }

                C P2
                {
                    get => {|CsWinRT1034:(C)_obj|};
                    set
                    {
                        E e = {|CsWinRT1035:(E)_obj|};
                        F f1 = {|CsWinRT1034:(F)_obj|};
                        F f2 = {|CsWinRT1034:(F)_obj|};
                        G g = {|CsWinRT1034:(G)_obj|};

                        _obj = value;
                    }
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

            [WindowsRuntimeType("SomeContract")]
            class F : C;

            [WindowsRuntimeType("SomeContract")]
            class G : F;
            """;

        const string @fixed = """
            using WinRT;

            namespace MyApp;

            public class Program
            {
                private object _obj;

                [DynamicWindowsRuntimeCast(typeof(C))]
                [DynamicWindowsRuntimeCast(typeof(D))]
                [DynamicWindowsRuntimeCast(typeof(E))]
                [DynamicWindowsRuntimeCast(typeof(F))]
                public void M(object obj)
                {
                    C c1 = (C)obj;
                    C c2 = (C)obj;
                    D d = (D)obj;
                    E e1 = (E)obj;
                    E e2 = (E)obj;
                    F f1 = (F)obj;
                    F f2 = (F)obj;
                    E e3 = (E)obj;
                }

                C P1
                {
                    [DynamicWindowsRuntimeCast(typeof(C))]
                    get => (C)_obj;
                }

                C P2
                {
                    [DynamicWindowsRuntimeCast(typeof(C))]
                    get => (C)_obj;
                    [DynamicWindowsRuntimeCast(typeof(E))]
                    [DynamicWindowsRuntimeCast(typeof(F))]
                    [DynamicWindowsRuntimeCast(typeof(G))]
                    set
                    {
                        E e = (E)_obj;
                        F f1 = (F)_obj;
                        F f2 = (F)_obj;
                        G g = (G)_obj;

                        _obj = value;
                    }
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

            [WindowsRuntimeType("SomeContract")]
            class F : C;

            [WindowsRuntimeType("SomeContract")]
            class G : F;
            """;

        CSharpCodeFixTest test = new(LanguageVersion.CSharp13, editorconfig: [("CsWinRTAotWarningLevel", "3")])
        {
            TestCode = original,
            FixedCode = @fixed
        };

        await test.RunAsync();
    }
}