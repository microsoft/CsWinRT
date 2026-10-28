// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using WindowsRuntime.SourceGenerator.Diagnostics;
using WindowsRuntime.SourceGenerator.Tests.Helpers;

namespace WindowsRuntime.SourceGenerator.Tests;

using VerifyCS = CSharpAnalyzerTest<WindowsRuntimeActivationFactoryAttributeAnalyzer>;

/// <summary>
/// Tests for <see cref="WindowsRuntimeActivationFactoryAttributeAnalyzer"/>.
/// </summary>
[TestClass]
public sealed class Test_WindowsRuntimeActivationFactoryAttributeAnalyzer
{
    /// <summary>
    /// Stands in for a reference projection built with <c>CsWinRTImplementWinMDTypes</c>: the projected classes, plus
    /// the generated bases for a default-activatable class, a class with factory methods, and a statics-only class.
    /// </summary>
    private const string ImplementableClasses = """
        using WindowsRuntime;
        using WindowsRuntime.InteropServices;

        [assembly: WindowsRuntimeReferenceAssemblyMetadata("CsWinRT.ImplementableClass.v1", "Contoso.Widgets.Widget")]
        [assembly: WindowsRuntimeReferenceAssemblyMetadata("CsWinRT.ImplementableClass.v1", "Contoso.Widgets.WidgetUtils")]
        [assembly: WindowsRuntimeReferenceAssemblyMetadata("CsWinRT.ImplementableClass.v1", "Contoso.Widgets.Gadget")]
        [assembly: WindowsRuntimeReferenceAssemblyMetadata("CsWinRT.ImplementableClass.v1", "Contoso.Widgets.Gizmo")]

        namespace Contoso.Widgets
        {
            public sealed class Widget;

            public static class WidgetUtils;
        }

        namespace ABI.Contoso.Widgets
        {
            public abstract class Widget
            {
                public abstract void DoStuff();
            }

            public abstract class WidgetActivationFactory : IActivationFactory
            {
                object IActivationFactory.ActivateInstance() => ActivateInstance();

                public abstract Widget ActivateInstance();
            }

            public abstract class WidgetUtilsActivationFactory
            {
                public abstract int Compute();
            }

            public abstract class Gadget;

            public abstract class GadgetActivationFactory
            {
                public abstract Gadget CreateWithName(string name);
            }

            public abstract class Gizmo;
        }

        namespace MyApp
        {
            public sealed class MyWidget : global::ABI.Contoso.Widgets.Widget
            {
                public override void DoStuff() { }
            }
        }

        """;

    // --- Implemented activation factories that should not warn ---

    [TestMethod]
    public async Task ImplementedFactory_ForImplementation_DoesNotWarn()
    {
        const string source = ImplementableClasses + """
            namespace MyApp
            {
                [WindowsRuntimeActivationFactory(typeof(MyWidget))]
                public sealed class MyWidgetFactory : global::ABI.Contoso.Widgets.WidgetActivationFactory
                {
                    public override global::ABI.Contoso.Widgets.Widget ActivateInstance() => new MyWidget();
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task ImplementedFactory_ForProjectedClass_DoesNotWarn()
    {
        // A statics-only class has no implementation, so the projected class is the only type that can name it
        const string source = ImplementableClasses + """
            namespace MyApp
            {
                [WindowsRuntimeActivationFactory(typeof(global::Contoso.Widgets.WidgetUtils))]
                internal sealed class MyWidgetUtilsFactory : global::ABI.Contoso.Widgets.WidgetUtilsActivationFactory
                {
                    public override int Compute() => 42;
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    // --- Implemented activation factories that should warn ---

    [TestMethod]
    public async Task StaticClass_Warns()
    {
        const string source = ImplementableClasses + """
            namespace MyApp
            {
                [{|CSWINRT2023:WindowsRuntimeActivationFactory(typeof(MyWidget))|}]
                public static class MyWidgetFactory;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task NonAbstractClassWithoutFactoryBase_Warns()
    {
        const string source = ImplementableClasses + """
            namespace MyApp
            {
                [{|CSWINRT2023:WindowsRuntimeActivationFactory(typeof(MyWidget))|}]
                public sealed class MyWidgetFactory : IActivationFactory
                {
                    public object ActivateInstance() => new MyWidget();
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task AbstractImplementedFactory_Warns()
    {
        const string source = ImplementableClasses + """
            namespace MyApp
            {
                [{|CSWINRT2023:WindowsRuntimeActivationFactory(typeof(MyWidget))|}]
                public abstract class MyWidgetFactory : global::ABI.Contoso.Widgets.WidgetActivationFactory;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task GenericImplementedFactory_Warns()
    {
        const string source = ImplementableClasses + """
            namespace MyApp
            {
                [{|CSWINRT2023:WindowsRuntimeActivationFactory(typeof(MyWidget))|}]
                public sealed class MyWidgetFactory<T> : global::ABI.Contoso.Widgets.WidgetActivationFactory
                {
                    public override global::ABI.Contoso.Widgets.Widget ActivateInstance() => new MyWidget();
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task PrivateImplementedFactory_Warns()
    {
        const string source = ImplementableClasses + """
            namespace MyApp
            {
                internal static class Outer
                {
                    [{|CSWINRT2023:WindowsRuntimeActivationFactory(typeof(MyWidget))|}]
                    private sealed class MyWidgetFactory : global::ABI.Contoso.Widgets.WidgetActivationFactory
                    {
                        public override global::ABI.Contoso.Widgets.Widget ActivateInstance() => new MyWidget();
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task ImplementedFactoryWithoutAccessibleParameterlessConstructor_Warns()
    {
        const string source = ImplementableClasses + """
            namespace MyApp
            {
                [{|CSWINRT2023:WindowsRuntimeActivationFactory(typeof(MyWidget))|}]
                public sealed class MyWidgetFactory : global::ABI.Contoso.Widgets.WidgetActivationFactory
                {
                    private MyWidgetFactory() { }

                    public override global::ABI.Contoso.Widgets.Widget ActivateInstance() => new MyWidget();
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    [DataRow("file sealed class MyWidgetFactory : global::ABI.Contoso.Widgets.WidgetActivationFactory { public override global::ABI.Contoso.Widgets.Widget ActivateInstance() => new MyWidget(); }")]
    [DataRow("public sealed class MyWidgetFactory : global::ABI.Contoso.Widgets.WidgetActivationFactory { public required int Value { get; set; } public override global::ABI.Contoso.Widgets.Widget ActivateInstance() => new MyWidget(); }")]
    public async Task ImplementedFactory_NotInstantiableFromGeneratedCode_Warns(string declaration)
    {
        // The generated activation entry point is in another file, and constructs the factory with 'new()'
        string source = ImplementableClasses + $$"""
            namespace MyApp
            {
                [{|CSWINRT2023:WindowsRuntimeActivationFactory(typeof(MyWidget))|}]
                {{declaration}}
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task ImplementedFactory_ForUnrelatedType_Warns()
    {
        const string source = ImplementableClasses + """
            namespace MyApp
            {
                [WindowsRuntimeActivationFactory({|CSWINRT2024:typeof(MyWidgetFactory)|})]
                public sealed class MyWidgetFactory : global::ABI.Contoso.Widgets.WidgetActivationFactory
                {
                    public override global::ABI.Contoso.Widgets.Widget ActivateInstance() => new MyWidget();
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task ImplementedFactory_ForImplementationOfAnotherClass_Warns()
    {
        const string source = ImplementableClasses + """
            namespace MyApp
            {
                [WindowsRuntimeActivationFactory({|CSWINRT2024:typeof(MyWidget)|})]
                public sealed class MyWidgetUtilsFactory : global::ABI.Contoso.Widgets.WidgetUtilsActivationFactory
                {
                    public override int Compute() => 42;
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task DuplicateImplementedFactories_Warn()
    {
        const string source = ImplementableClasses + """
            namespace MyApp
            {
                [{|CSWINRT2025:WindowsRuntimeActivationFactory(typeof(MyWidget))|}]
                public sealed class MyWidgetFactory : global::ABI.Contoso.Widgets.WidgetActivationFactory
                {
                    public override global::ABI.Contoso.Widgets.Widget ActivateInstance() => new MyWidget();
                }

                [{|CSWINRT2025:WindowsRuntimeActivationFactory(typeof(global::Contoso.Widgets.Widget))|}]
                public sealed class OtherWidgetFactory : global::ABI.Contoso.Widgets.WidgetActivationFactory
                {
                    public override global::ABI.Contoso.Widgets.Widget ActivateInstance() => new MyWidget();
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    // --- Generated activation factories that should not warn ---

    [TestMethod]
    public async Task GeneratedFactory_WithRequiredMembersSetByConstructor_DoesNotWarn()
    {
        const string source = ImplementableClasses + """
            namespace MyApp
            {
                [GeneratedWindowsRuntimeActivationFactory]
                public sealed class AnnotatedWidget : global::ABI.Contoso.Widgets.Widget
                {
                    [System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
                    public AnnotatedWidget() => Value = 1;

                    public required int Value { get; set; }

                    public override void DoStuff() { }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task GeneratedFactory_WithInheritedRequiredMembers_Warns()
    {
        const string source = ImplementableClasses + """
            namespace MyApp
            {
                public abstract class RequiredWidget : global::ABI.Contoso.Widgets.Widget
                {
                    public required int Value { get; set; }
                }

                [{|CSWINRT2027:GeneratedWindowsRuntimeActivationFactory|}]
                public sealed class AnnotatedWidget : RequiredWidget
                {
                    public override void DoStuff() { }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task GeneratedFactory_InGeneratedCode_CountsTowardsDuplicates()
    {
        // The source generator picks up requests in generated code as well, so the duplicate is reported on the other one
        const string generatedSource = """
            namespace MyApp
            {
                [WindowsRuntime.InteropServices.GeneratedWindowsRuntimeActivationFactory]
                public sealed class GeneratedWidget : global::ABI.Contoso.Widgets.Widget
                {
                    public override void DoStuff() { }
                }
            }
            """;

        const string source = ImplementableClasses + """
            namespace MyApp
            {
                [{|CSWINRT2025:GeneratedWindowsRuntimeActivationFactory|}]
                public sealed class AnnotatedWidget : global::ABI.Contoso.Widgets.Widget
                {
                    public override void DoStuff() { }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, generatedSource: generatedSource);
    }

    [TestMethod]
    public async Task GeneratedFactory_ForDefaultActivatableClass_DoesNotWarn()
    {
        const string source = ImplementableClasses + """
            namespace MyApp
            {
                [GeneratedWindowsRuntimeActivationFactory]
                internal sealed class AnnotatedWidget : global::ABI.Contoso.Widgets.Widget
                {
                    public override void DoStuff() { }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    // --- Generated activation factories that should warn ---

    [TestMethod]
    [DataRow("public sealed class AnnotatedWidget;")]
    [DataRow("public abstract class AnnotatedWidget : global::ABI.Contoso.Widgets.Widget;")]
    [DataRow("public sealed class AnnotatedWidget<T> : global::ABI.Contoso.Widgets.Widget { public override void DoStuff() { } }")]
    [DataRow("public sealed class AnnotatedWidget : global::ABI.Contoso.Widgets.Widget { public AnnotatedWidget(int value) { } public override void DoStuff() { } }")]
    [DataRow("public sealed class AnnotatedGizmo : global::ABI.Contoso.Widgets.Gizmo;")]
    [DataRow("public sealed class AnnotatedGadget : global::ABI.Contoso.Widgets.Gadget;")]
    [DataRow("file sealed class AnnotatedWidget : global::ABI.Contoso.Widgets.Widget { public override void DoStuff() { } }")]
    [DataRow("public sealed class AnnotatedWidget : global::ABI.Contoso.Widgets.Widget { public required int Value { get; set; } public override void DoStuff() { } }")]
    public async Task InvalidGeneratedFactory_Warns(string declaration)
    {
        string source = ImplementableClasses + $$"""
            namespace MyApp
            {
                [{|CSWINRT2027:GeneratedWindowsRuntimeActivationFactory|}]
                {{declaration}}
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task PrivateGeneratedFactory_Warns()
    {
        const string source = ImplementableClasses + """
            namespace MyApp
            {
                internal static class Outer
                {
                    [{|CSWINRT2027:GeneratedWindowsRuntimeActivationFactory|}]
                    private sealed class AnnotatedWidget : global::ABI.Contoso.Widgets.Widget
                    {
                        public override void DoStuff() { }
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task GeneratedFactory_WithDeclaredFactory_Warns()
    {
        const string source = ImplementableClasses + """
            namespace MyApp
            {
                [{|CSWINRT2025:GeneratedWindowsRuntimeActivationFactory|}]
                public sealed class AnnotatedWidget : global::ABI.Contoso.Widgets.Widget
                {
                    public override void DoStuff() { }
                }

                [{|CSWINRT2025:WindowsRuntimeActivationFactory(typeof(MyWidget))|}]
                public sealed class MyWidgetFactory : global::ABI.Contoso.Widgets.WidgetActivationFactory
                {
                    public override global::ABI.Contoso.Widgets.Widget ActivateInstance() => new MyWidget();
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    public async Task GeneratedFactories_OnSeveralImplementations_Warn()
    {
        const string source = ImplementableClasses + """
            namespace MyApp
            {
                [{|CSWINRT2025:GeneratedWindowsRuntimeActivationFactory|}]
                public sealed class AnnotatedWidget : global::ABI.Contoso.Widgets.Widget
                {
                    public override void DoStuff() { }
                }

                [{|CSWINRT2025:GeneratedWindowsRuntimeActivationFactory|}]
                public sealed class OtherAnnotatedWidget : global::ABI.Contoso.Widgets.Widget
                {
                    public override void DoStuff() { }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    // --- Activation factory extensions that should not warn ---

    [TestMethod]
    [DataRow("public")]
    [DataRow("protected")]
    [DataRow("protected internal")]
    public async Task FactoryExtension_DoesNotWarn(string constructorAccessibility)
    {
        string source = $$"""
            using WindowsRuntime.InteropServices;

            namespace MyComponent
            {
                public sealed class Greeter;

                [WindowsRuntimeActivationFactory(typeof(Greeter))]
                public abstract class GreeterFactoryExtensions
                {
                    {{constructorAccessibility}} GreeterFactoryExtensions() { }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task FactoryExtension_ForStaticsOnlyClass_DoesNotWarn()
    {
        const string source = """
            using WindowsRuntime.InteropServices;

            namespace MyComponent
            {
                public static class GreeterUtils
                {
                    public static int Compute() => 42;
                }

                public static class Outer
                {
                    [WindowsRuntimeActivationFactory(typeof(GreeterUtils))]
                    public abstract class GreeterUtilsFactoryExtensions;
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    // --- Activation factory extensions that should warn ---

    [TestMethod]
    public async Task FactoryExtension_NotInComponent_Warns()
    {
        const string source = """
            using WindowsRuntime.InteropServices;

            namespace MyComponent
            {
                public sealed class Greeter;

                [{|CSWINRT2023:WindowsRuntimeActivationFactory(typeof(Greeter))|}]
                public abstract class GreeterFactoryExtensions;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source);
    }

    [TestMethod]
    [DataRow("""
        [{|CSWINRT2023:WindowsRuntimeActivationFactory(typeof(Greeter))|}]
        internal abstract class GreeterFactoryExtensions;
        """)]
    [DataRow("""
        internal static class Outer
        {
            [{|CSWINRT2023:WindowsRuntimeActivationFactory(typeof(Greeter))|}]
            public abstract class GreeterFactoryExtensions;
        }
        """)]
    [DataRow("""
        [{|CSWINRT2023:WindowsRuntimeActivationFactory(typeof(Greeter))|}]
        public abstract class GreeterFactoryExtensions<T>;
        """)]
    [DataRow("""
        [{|CSWINRT2023:WindowsRuntimeActivationFactory(typeof(Greeter))|}]
        public abstract class GreeterFactoryExtensions
        {
            private protected GreeterFactoryExtensions() { }
        }
        """)]
    [DataRow("""
        [{|CSWINRT2023:WindowsRuntimeActivationFactory(typeof(Greeter))|}]
        public abstract class GreeterFactoryExtensions
        {
            public abstract void Initialize();
        }
        """)]
    public async Task InvalidFactoryExtension_Warns(string declaration)
    {
        string source = $$"""
            using WindowsRuntime.InteropServices;

            namespace MyComponent
            {
                public sealed class Greeter;

                {{declaration}}
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    [DataRow("typeof(string)")]
    [DataRow("typeof(InternalGreeter)")]
    [DataRow("typeof(IGreeter)")]
    [DataRow("typeof(Outer.NestedGreeter)")]
    [DataRow("typeof(NonActivatableGreeter)")]
    [DataRow("typeof(IgnoredGreeter)")]
    [DataRow("typeof(FilteredGreeter)")]
    public async Task FactoryExtension_ForNonActivatableAuthoredClass_Warns(string runtimeClassType)
    {
        string source = $$"""
            using WindowsRuntime.InteropServices;

            namespace MyComponent
            {
                internal sealed class InternalGreeter;

                [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata]
                public sealed class IgnoredGreeter;

                public sealed class FilteredGreeter
                {
                    [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata]
                    public FilteredGreeter() { }

                    [WindowsRuntime.InteropServices.ExcludeFromWindowsMetadata]
                    public static void Helper() { }
                }

                public interface IGreeter;

                public static class Outer
                {
                    public sealed class NestedGreeter;
                }

                public sealed class NonActivatableGreeter
                {
                    private NonActivatableGreeter() { }

                    public void Greet() { }
                }

                [WindowsRuntimeActivationFactory({|CSWINRT2024:{{runtimeClassType}}|})]
                public abstract class GreeterFactoryExtensions;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }

    [TestMethod]
    public async Task DuplicateFactoryExtensions_Warn()
    {
        const string source = """
            using WindowsRuntime.InteropServices;

            namespace MyComponent
            {
                public sealed class Greeter;

                [{|CSWINRT2025:WindowsRuntimeActivationFactory(typeof(Greeter))|}]
                public abstract class FirstGreeterFactoryExtensions;

                [{|CSWINRT2025:WindowsRuntimeActivationFactory(typeof(Greeter))|}]
                public abstract class SecondGreeterFactoryExtensions;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(source, isCsWinRTComponent: true);
    }
}