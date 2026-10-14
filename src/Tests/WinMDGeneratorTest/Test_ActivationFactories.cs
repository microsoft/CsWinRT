// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WinMDGeneratorTest.Helpers;

namespace WinMDGeneratorTest;

/// <summary>
/// End-to-end tests for how the WinMD generator handles activation factories declared with <c>[WindowsRuntimeActivationFactory]</c>.
/// </summary>
[TestClass]
public class Test_ActivationFactories
{
    /// <summary>
    /// An activation factory is not a runtime class, whether it extends the one CsWinRT generates (an abstract class)
    /// or is a complete one (as written when implementing a class declared in existing metadata).
    /// </summary>
    [TestMethod]
    public void ActivationFactories_AreNotRuntimeClasses()
    {
        string[] types = WinMDGeneratorRunner.GetGeneratedTypeNames("""
            using WindowsRuntime.InteropServices;

            namespace WindowsRuntime.InteropServices
            {
                [System.AttributeUsage(System.AttributeTargets.Class)]
                internal sealed class WindowsRuntimeActivationFactoryAttribute(System.Type runtimeClassType) : System.Attribute;
            }

            namespace Contoso
            {
                public sealed class Widget
                {
                }

                [WindowsRuntimeActivationFactory(typeof(Widget))]
                public abstract class WidgetFactoryExtensions
                {
                }

                [WindowsRuntimeActivationFactory(typeof(Widget))]
                public sealed class WidgetFactory
                {
                }
            }
            """);

        CollectionAssert.Contains(types, "Contoso.Widget");
        CollectionAssert.DoesNotContain(types, "Contoso.WidgetFactoryExtensions");
        CollectionAssert.DoesNotContain(types, "Contoso.WidgetFactory");
    }
}
