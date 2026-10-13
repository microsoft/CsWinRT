// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using WindowsRuntime.SourceGenerator.Diagnostics;
using WindowsRuntime.SourceGenerator.Tests.Helpers;

namespace WindowsRuntime.SourceGenerator.Tests;

using VerifyCS = CSharpAnalyzerTest<ImplementedRuntimeClassActivationAnalyzer>;

/// <summary>
/// Tests for <see cref="ImplementedRuntimeClassActivationAnalyzer"/>.
/// </summary>
[TestClass]
public sealed class Test_ImplementedRuntimeClassActivationAnalyzer
{
    /// <summary>
    /// Stands in for a reference projection built with <c>CsWinRTImplementWinMDTypes</c>: the generated bases for a
    /// default-activatable class, a class with factory methods, and a class that is not activatable at all.
    /// </summary>
    private const string ImplementableClasses = """
        using WindowsRuntime;
        using WindowsRuntime.InteropServices;

        [assembly: WindowsRuntimeReferenceAssemblyMetadata("CsWinRT.ImplementableClass.v1", "Contoso.Widgets.Widget")]
        [assembly: WindowsRuntimeReferenceAssemblyMetadata("CsWinRT.ImplementableClass.v1", "Contoso.Widgets.Gadget")]
        [assembly: WindowsRuntimeReferenceAssemblyMetadata("CsWinRT.ImplementableClass.v1", "Contoso.Widgets.Gizmo")]

        namespace ABI.Contoso.Widgets
        {
            public abstract class Widget;

            public abstract class WidgetActivationFactory : IActivationFactory
            {
                object IActivationFactory.ActivateInstance() => ActivateInstance();

                public abstract Widget ActivateInstance();
            }

            public abstract class Gadget;

            public abstract class GadgetActivationFactory : IActivationFactory
            {
                object IActivationFactory.ActivateInstance() => ActivateInstance();

                public abstract Gadget ActivateInstance();

                public abstract Gadget CreateWithName(string name);
            }

            public abstract class Gizmo;
        }

        """;

    // --- Tests where the analyzer should NOT warn ---

    [TestMethod]
    public async Task DefaultActivatableClass_WithGeneratedFactoryAttribute_DoesNotWarn()
    {
        const string source = ImplementableClasses + """
            namespace MyApp
            {
                [GeneratedWindowsRuntimeActivationFactory]
                internal class MyWidget : global::ABI.Contoso.Widgets.Widget;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task MultipleImplementations_OneWithGeneratedFactoryAttribute_DoesNotWarn()
    {
        // The class is activatable through the annotated implementation, which is all that is needed
        const string source = ImplementableClasses + """
            namespace MyApp
            {
                [GeneratedWindowsRuntimeActivationFactory]
                public sealed class MyWidget : global::ABI.Contoso.Widgets.Widget;

                public sealed class OtherWidget : global::ABI.Contoso.Widgets.Widget;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task InvalidGeneratedFactoryAttribute_DoesNotWarn()
    {
        // Requesting a factory that cannot be generated is reported on the attribute ('CSWINRT2027') instead
        const string source = ImplementableClasses + """
            namespace MyApp
            {
                [GeneratedWindowsRuntimeActivationFactory]
                public sealed class MyGadget : global::ABI.Contoso.Widgets.Gadget;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task ClassWithDeclaredFactory_DoesNotWarn()
    {
        const string source = ImplementableClasses + """
            namespace MyApp
            {
                public sealed class MyGadget : global::ABI.Contoso.Widgets.Gadget;

                public sealed class OtherGadget : global::ABI.Contoso.Widgets.Gadget;

                [WindowsRuntimeActivationFactory(typeof(MyGadget))]
                public sealed class MyGadgetFactory : global::ABI.Contoso.Widgets.GadgetActivationFactory
                {
                    public override global::ABI.Contoso.Widgets.Gadget ActivateInstance() => new MyGadget();

                    public override global::ABI.Contoso.Widgets.Gadget CreateWithName(string name) => new MyGadget();
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task NonActivatableClass_DoesNotWarn()
    {
        const string source = ImplementableClasses + """
            namespace MyApp
            {
                public sealed class MyGizmo : global::ABI.Contoso.Widgets.Gizmo;

                public sealed class OtherGizmo : global::ABI.Contoso.Widgets.Gizmo;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task AbstractImplementation_DoesNotWarn()
    {
        const string source = ImplementableClasses + """
            namespace MyApp
            {
                public abstract class MyGadgetBase : global::ABI.Contoso.Widgets.Gadget;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task GenericImplementation_WithAnnotatedImplementation_DoesNotWarn()
    {
        // The annotated implementation makes the class activatable, so the generic one is fine too
        const string source = ImplementableClasses + """
            namespace MyApp
            {
                [GeneratedWindowsRuntimeActivationFactory]
                public sealed class MyWidget : global::ABI.Contoso.Widgets.Widget;

                public sealed class MyWidget<T> : global::ABI.Contoso.Widgets.Widget;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task GeneratedFactory_InGeneratedCode_DoesNotWarn()
    {
        // The source generator picks up requests in generated code as well, which makes the class activatable
        const string generatedSource = """
            namespace MyApp
            {
                [WindowsRuntime.InteropServices.GeneratedWindowsRuntimeActivationFactory]
                internal sealed class GeneratedWidget : global::ABI.Contoso.Widgets.Widget;
            }
            """;

        const string source = ImplementableClasses + """
            namespace MyApp
            {
                public sealed class MyWidget : global::ABI.Contoso.Widgets.Widget;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, generatedSource: generatedSource);
    }

    // --- Tests where the analyzer SHOULD warn ---

    [TestMethod]
    [DataRow("""
        [WindowsRuntime.InteropServices.GeneratedWindowsRuntimeActivationFactory]
        internal sealed class GeneratedWidget : global::ABI.Contoso.Widgets.Widget
        {
            public required int Value { get; set; }
        }
        """)]
    [DataRow("""
        [WindowsRuntime.InteropServices.WindowsRuntimeActivationFactory(typeof(string))]
        internal sealed class GeneratedWidgetFactory : global::ABI.Contoso.Widgets.WidgetActivationFactory
        {
            public override global::ABI.Contoso.Widgets.Widget ActivateInstance() => null!;
        }
        """)]
    public async Task InvalidFactoryInGeneratedCode_Warns(string generatedDeclaration)
    {
        // Diagnostics are not reported in generated code, so an invalid factory there must not hide the missing factory
        string generatedSource = $$"""
            namespace MyApp
            {
                {{generatedDeclaration}}
            }
            """;

        const string source = ImplementableClasses + """
            namespace MyApp
            {
                public sealed class {|CSWINRT2026:MyWidget|} : global::ABI.Contoso.Widgets.Widget;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, generatedSource: generatedSource);
    }

    [TestMethod]
    public async Task DefaultActivatableClass_WithoutGeneratedFactoryAttribute_Warns()
    {
        // The generated factory is opt-in, so a single implementation is not enough on its own
        const string source = ImplementableClasses + """
            namespace MyApp
            {
                internal class {|CSWINRT2026:MyWidget|} : global::ABI.Contoso.Widgets.Widget;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task ClassWithFactoryMethods_WithoutDeclaredFactory_Warns()
    {
        const string source = ImplementableClasses + """
            namespace MyApp
            {
                public sealed class {|CSWINRT2026:MyGadget|} : global::ABI.Contoso.Widgets.Gadget;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task MultipleImplementations_Warn()
    {
        const string source = ImplementableClasses + """
            namespace MyApp
            {
                public sealed class {|CSWINRT2026:MyWidget|} : global::ABI.Contoso.Widgets.Widget;

                public sealed class {|CSWINRT2026:OtherWidget|} : global::ABI.Contoso.Widgets.Widget;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task GenericImplementation_Warns()
    {
        const string source = ImplementableClasses + """
            namespace MyApp
            {
                public sealed class {|CSWINRT2026:MyWidget|}<T> : global::ABI.Contoso.Widgets.Widget;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task PrivateImplementation_Warns()
    {
        const string source = ImplementableClasses + """
            namespace MyApp
            {
                internal static class Outer
                {
                    private sealed class {|CSWINRT2026:MyWidget|} : global::ABI.Contoso.Widgets.Widget;
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task ImplementationWithoutAccessibleParameterlessConstructor_Warns()
    {
        const string source = ImplementableClasses + """
            namespace MyApp
            {
                public sealed class {|CSWINRT2026:MyWidget|} : global::ABI.Contoso.Widgets.Widget
                {
                    public MyWidget(int value) { }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }
}
