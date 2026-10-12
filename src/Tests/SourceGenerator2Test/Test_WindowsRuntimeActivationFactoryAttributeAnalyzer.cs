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
    public async Task FactoryExtension_ForNonActivatableAuthoredClass_Warns(string runtimeClassType)
    {
        string source = $$"""
            using WindowsRuntime.InteropServices;

            namespace MyComponent
            {
                internal sealed class InternalGreeter;

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
