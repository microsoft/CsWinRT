// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using WindowsRuntime.SourceGenerator.Diagnostics;
using WindowsRuntime.SourceGenerator.Tests.Helpers;

namespace WindowsRuntime.SourceGenerator.Tests;

using VerifyCS = CSharpAnalyzerTest<ObsoleteWithoutDeprecatedAnalyzer>;

/// <summary>
/// Tests for <see cref="ObsoleteWithoutDeprecatedAnalyzer"/>.
/// </summary>
[TestClass]
public sealed class Test_ObsoleteWithoutDeprecatedAnalyzer
{
    [TestMethod]
    public async Task PublicClass_NoAttributes_DoesNotWarn()
    {
        const string source = """
            public sealed class MyClass;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task PublicClass_OnlyDeprecated_DoesNotWarn()
    {
        const string source = """
            using Windows.Foundation.Metadata;

            [Deprecated("Use MyOtherClass instead", DeprecationType.Deprecate, 1u)]
            public sealed class MyClass;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task PublicClass_ObsoleteAndDeprecated_DoesNotWarn()
    {
        const string source = """
            using System;
            using Windows.Foundation.Metadata;

            [Obsolete("Use MyOtherClass instead")]
            [Deprecated("Use MyOtherClass instead", DeprecationType.Deprecate, 1u)]
            public sealed class MyClass;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task PublicClass_OnlyObsolete_NotComponent_DoesNotWarn()
    {
        const string source = """
            using System;

            [Obsolete("Use MyOtherClass instead")]
            public sealed class MyClass;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task InternalClass_OnlyObsolete_DoesNotWarn()
    {
        const string source = """
            using System;

            [Obsolete("Use MyOtherClass instead")]
            internal sealed class MyClass;
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task NestedPublicClass_OnlyObsolete_DoesNotWarn()
    {
        // Windows Runtime has no nested types, so a nested type never reaches the '.winmd'
        const string source = """
            using System;

            public sealed class Outer
            {
                [Obsolete("Use something else instead")]
                public sealed class Nested;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task NonPublicMember_OnlyObsolete_DoesNotWarn()
    {
        const string source = """
            using System;

            public sealed class MyClass
            {
                [Obsolete("Use NewMethod instead")]
                internal void OldMethod()
                {
                }

                [Obsolete("Use NewProperty instead")]
                private int OldProperty => 42;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task PublicMemberOfInternalType_OnlyObsolete_DoesNotWarn()
    {
        const string source = """
            using System;

            internal sealed class MyClass
            {
                [Obsolete("Use NewMethod instead")]
                public void OldMethod()
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    [DataRow("class")]
    [DataRow("struct")]
    public async Task PublicType_OnlyObsolete_Warns(string typeKeyword)
    {
        string source = $$"""
            using System;

            [Obsolete("Use MyOtherType instead")]
            public {{typeKeyword}} {|CSWINRT2022:MyType|};
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task PublicInterface_OnlyObsolete_Warns()
    {
        const string source = """
            using System;

            [Obsolete("Use IMyOtherInterface instead")]
            public interface {|CSWINRT2022:IMyInterface|};
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task PublicEnum_OnlyObsolete_Warns()
    {
        const string source = """
            using System;

            [Obsolete("Use MyOtherEnum instead")]
            public enum {|CSWINRT2022:MyEnum|}
            {
                A,
                B
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task PublicDelegate_OnlyObsolete_Warns()
    {
        const string source = """
            using System;

            [Obsolete("Use MyOtherDelegate instead")]
            public delegate void {|CSWINRT2022:MyDelegate|}();
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task PublicMembers_OnlyObsolete_Warn()
    {
        // '[Deprecated]' is supported on members too, so '[Obsolete]' is just as ineffective there
        const string source = """
            using System;

            public sealed class MyClass
            {
                [Obsolete("Use NewMethod instead")]
                public void {|CSWINRT2022:OldMethod|}()
                {
                }

                [Obsolete("Use NewProperty instead")]
                public int {|CSWINRT2022:OldProperty|} => 42;

                [Obsolete("Use NewEvent instead")]
                public event EventHandler<int> {|CSWINRT2022:OldEvent|};
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task PublicMembers_ObsoleteAndDeprecated_DoNotWarn()
    {
        const string source = """
            using System;
            using Windows.Foundation.Metadata;

            public sealed class MyClass
            {
                [Obsolete("Use NewMethod instead")]
                [Deprecated("Use NewMethod instead", DeprecationType.Deprecate, 1u)]
                public void OldMethod()
                {
                }

                [Obsolete("Use NewProperty instead")]
                [Deprecated("Use NewProperty instead", DeprecationType.Deprecate, 1u)]
                public int OldProperty => 42;

                [Obsolete("Use NewEvent instead")]
                [Deprecated("Use NewEvent instead", DeprecationType.Deprecate, 1u)]
                public event EventHandler<int> OldEvent;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task PublicConstructor_OnlyObsolete_DoesNotWarn()
    {
        // '[Deprecated]' does not include 'AttributeTargets.Constructor' in its usage, so it cannot be
        // applied to a constructor at all: there would be no way to act on the diagnostic
        const string source = """
            using System;

            public sealed class MyClass
            {
                [Obsolete("Use the parameterless constructor instead")]
                public MyClass(int value)
                {
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task ObsoleteOnPropertyAccessor_DoesNotWarn()
    {
        // Accessors are exported as part of their property, and the generator only ever moves a
        // '[Deprecated]' from the property down onto the accessor row, never the other way around
        const string source = """
            using System;

            public sealed class MyClass
            {
                public int OldProperty
                {
                    [Obsolete("Use NewProperty instead")]
                    get;

                    set;
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task StructMembers_OnlyObsolete_DoNotWarn()
    {
        // A Windows Runtime struct is a plain field aggregate: the generator drops every member of one
        // other than its public instance fields, so those members never reach the '.winmd'
        const string source = """
            using System;

            public struct MyStruct
            {
                public int Value;

                [Obsolete("Use Value instead")]
                public int GetValue()
                {
                    return Value;
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task PublicInterfaceMembers_OnlyObsolete_Warn()
    {
        // Interface members are implicitly public, so they are exported with the interface
        const string source = """
            using System;

            public interface IMyInterface
            {
                [Obsolete("Use NewMethod instead")]
                void {|CSWINRT2022:OldMethod|}();

                [Obsolete("Use NewProperty instead")]
                int {|CSWINRT2022:OldProperty|} { get; }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task EnumMember_OnlyObsolete_Warns()
    {
        // Windows Runtime metadata carries member markers on individual enum members, and the generator
        // copies the attributes of every enum member over, so an '[Obsolete]' there is just as ineffective
        const string source = """
            using System;

            public enum MyEnum
            {
                A,

                [Obsolete("Use A instead")]
                {|CSWINRT2022:B|}
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task EnumMember_ObsoleteAndDeprecated_DoesNotWarn()
    {
        const string source = """
            using System;
            using Windows.Foundation.Metadata;

            public enum MyEnum
            {
                A,

                [Obsolete("Use A instead")]
                [Deprecated("Use A instead", DeprecationType.Deprecate, 1u)]
                B
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task StructInstanceField_OnlyObsolete_Warns()
    {
        // A struct's public instance fields are the one kind of member it does export, and the generator
        // copies their attributes over
        const string source = """
            using System;

            public struct MyStruct
            {
                [Obsolete("Use NewValue instead")]
                public int {|CSWINRT2022:OldValue|};

                public int NewValue;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task StructInstanceField_ObsoleteAndDeprecated_DoesNotWarn()
    {
        const string source = """
            using System;
            using Windows.Foundation.Metadata;

            public struct MyStruct
            {
                [Obsolete("Use NewValue instead")]
                [Deprecated("Use NewValue instead", DeprecationType.Deprecate, 1u)]
                public int OldValue;

                public int NewValue;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task NonExportedFields_OnlyObsolete_DoNotWarn()
    {
        // A struct only exports its public instance fields, and a class exports no field at all: a Windows
        // Runtime class is exposed purely through its interfaces, so none of these reach the '.winmd'
        const string source = """
            using System;

            public struct MyStruct
            {
                public int Value;

                [Obsolete("Use Value instead")]
                public static int OldStatic;

                [Obsolete("Use Value instead")]
                public const int OldConstant = 42;

                [Obsolete("Use Value instead")]
                internal int OldInternal;
            }

            public sealed class MyClass
            {
                [Obsolete("Use NewProperty instead")]
                public int OldField;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }
}
