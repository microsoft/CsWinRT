using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;

internal static class Program
{
    [STAThread]
    [DynamicDependency(nameof(InvalidateViewport))]
    private static int Main(string[] args)
    {
        if (typeof(IFrameworkElementProtected7).GUID != new Guid("65aa0480-22e3-5103-ad2a-b626f88ca5ae"))
        {
            return 1;
        }

        if (typeof(IFrameworkElementProtected7).Assembly.GetName().Name != "WinRT.Projection" ||
            typeof(FrameworkElement).Assembly.GetName().Name != "WinRT.Sdk.Xaml.Projection")
        {
            return 2;
        }

        Console.WriteLine(typeof(IFrameworkElementProtected7).FullName);

        // CI can run the type-load smoke test headlessly. An interactive desktop can also
        // exercise the real QI and viewport call by passing --xaml.
        if (args is ["--xaml"])
        {
            RunXamlTest();
        }
        else if (args.Length != 0)
        {
            throw new ArgumentException("The only supported argument is --xaml.");
        }

        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void InvalidateViewport(FrameworkElement element)
    {
        ((IFrameworkElementProtected7)element).InvalidateViewport();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RunXamlTest()
    {
        using WindowsXamlManager manager = WindowsXamlManager.InitializeForCurrentThread();
        InvalidateViewport(new ScrollContentPresenter());
        Console.WriteLine("FrameworkElement QI and InvalidateViewport succeeded.");
    }
}
