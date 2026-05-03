// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using WindowsRuntime.ProjectionGenerator.Writer;

namespace WindowsRuntime.ProjectionGenerator.Writer.TestRunner;

internal static class Program
{
    public static int Main(string[] args)
    {
        // Modes:
        //   1) Single .winmd path → simple test (legacy)
        //   2) "compare" <winmd-folder> <internal-winmd> <output-folder> → SDK projection mode
        //   3) "compare-xaml" <xaml-winmd-folder> <internal-winmd> <output-folder> → XAML projection mode
        //   4) "compare-authoring" <output-folder> → component projection mode (uses fixed paths
        //      that mirror the .rsp file in 'authoring-projection\generated-sources\ProjectionGenerator.rsp'
        //      shipped in the test inputs folder).
        if (args.Length >= 4 && args[0] == "compare")
        {
            return RunCompare(args[1], args[2], args[3]);
        }
        if (args.Length >= 4 && args[0] == "compare-xaml")
        {
            return RunCompareXaml(args[1], args[2], args[3]);
        }
        if (args.Length >= 2 && args[0] == "compare-authoring")
        {
            return RunCompareAuthoring(args[1]);
        }

        return RunSimple(args);
    }

    private static int RunSimple(string[] args)
    {
        string winmdPath = args.Length > 0 ? args[0] : @"C:\Program Files (x86)\Windows Kits\10\UnionMetadata\10.0.26100.0\Windows.winmd";
        string outputFolder = args.Length > 1 ? args[1] : Path.Combine(Path.GetTempPath(), "CsWinRTPort", $"out-{DateTime.UtcNow:yyyyMMddHHmmss}");

        if (!File.Exists(winmdPath))
        {
            Console.Error.WriteLine($"Input file not found: {winmdPath}");
            return 1;
        }

        Console.WriteLine($"Input:  {winmdPath}");
        Console.WriteLine($"Output: {outputFolder}");

        try
        {
            ProjectionWriter.Run(new ProjectionWriterOptions
            {
                InputPaths = new[] { winmdPath },
                OutputFolder = outputFolder,
                Include = new[] { "Windows", "WindowsRuntime.Internal" },
                Verbose = true,
            });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }

        if (Directory.Exists(outputFolder))
        {
            string[] files = Directory.GetFiles(outputFolder, "*.cs", SearchOption.AllDirectories);
            Console.WriteLine($"Generated {files.Length} files");
        }
        return 0;
    }

    /// <summary>
    /// Runs the projection writer with the same options the build pipeline uses for the SDK
    /// projection (the truth output to compare against).
    /// </summary>
    private static int RunCompare(string winmdFolder, string internalWinmd, string output)
    {
        if (Directory.Exists(output))
        {
            Directory.Delete(output, true);
        }
        _ = Directory.CreateDirectory(output);

        try
        {
            ProjectionWriter.Run(new ProjectionWriterOptions
            {
                InputPaths = new[] { winmdFolder, internalWinmd },
                OutputFolder = output,
                Include = new[]
                {
                    "Windows",
                    "WindowsRuntime.Internal",
                    "Windows.UI.Xaml.Interop",
                    "Windows.UI.Xaml.Data.BindableAttribute",
                    "Windows.UI.Xaml.Markup.ContentPropertyAttribute",
                },
                Exclude = new[]
                {
                    "Windows.UI.Colors",
                    "Windows.UI.ColorHelper",
                    "Windows.UI.IColorHelper",
                    "Windows.UI.IColors",
                    "Windows.UI.Text.FontWeights",
                    "Windows.UI.Text.IFontWeights",
                    "Windows.UI.Xaml",
                    "Windows.ApplicationModel.Store.Preview.WebAuthenticationCoreManagerHelper",
                    "Windows.ApplicationModel.Store.Preview.IWebAuthenticationCoreManagerHelper",
                },
            });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }

        Console.WriteLine($"Generated {Directory.GetFiles(output, "*.cs").Length} files in {output}");
        return 0;
    }

    /// <summary>
    /// Runs the projection writer with the same options the build pipeline uses for the XAML
    /// projection (the truth output to compare against). Mirrors the .rsp file in the XAML truth folder.
    /// </summary>
    private static int RunCompareXaml(string xamlWinmdFolder, string internalWinmd, string output)
    {
        if (Directory.Exists(output))
        {
            Directory.Delete(output, true);
        }
        _ = Directory.CreateDirectory(output);

        try
        {
            ProjectionWriter.Run(new ProjectionWriterOptions
            {
                InputPaths = new[] { xamlWinmdFolder, internalWinmd },
                OutputFolder = output,
                // Mirrors the XAML projection generation .rsp:
                // -exclude Windows, then -include for the specific XAML namespaces and helpers.
                Include = new[]
                {
                    "Windows.UI.Colors",
                    "Windows.UI.ColorHelper",
                    "Windows.UI.IColorHelper",
                    "Windows.UI.IColors",
                    "Windows.UI.Text.FontWeights",
                    "Windows.UI.Text.IFontWeights",
                    "Windows.UI.Xaml",
                    "Windows.ApplicationModel.Store.Preview.WebAuthenticationCoreManagerHelper",
                    "Windows.ApplicationModel.Store.Preview.IWebAuthenticationCoreManagerHelper",
                },
                Exclude = new[]
                {
                    "Windows",
                    "Windows.UI.Xaml.Interop",
                    "Windows.UI.Xaml.Data.BindableAttribute",
                    "Windows.UI.Xaml.Markup.ContentPropertyAttribute",
                },
            });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }

        Console.WriteLine($"Generated {Directory.GetFiles(output, "*.cs").Length} files in {output}");
        return 0;
    }

    /// <summary>
    /// Runs the projection writer with the same options the build pipeline uses for component
    /// authoring (the truth output to compare against). Mirrors the .rsp file in the authoring truth folder.
    /// </summary>
    private static int RunCompareAuthoring(string output)
    {
        const string AuthoringTestWinmd = @"C:\Users\sergiopedri\Downloads\authoring-projection\AuthoringTest.winmd";
        const string InternalWinmd = @"C:\Users\sergiopedri\.nuget\packages\microsoft.windows.cswinrt\3.0.0-prerelease-ci.260430.9\metadata\WindowsRuntime.Internal.winmd";
        const string AppSdkInteractive = @"C:\Users\sergiopedri\.nuget\packages\microsoft.windowsappsdk.interactiveexperiences\1.8.251104001\metadata\10.0.18362.0";
        const string AppSdkWinui = @"C:\Users\sergiopedri\.nuget\packages\microsoft.windowsappsdk.winui\1.8.251105000\metadata";
        const string AppSdkFoundation = @"C:\Users\sergiopedri\.nuget\packages\microsoft.windowsappsdk.foundation\1.8.251104000\metadata";
        const string WebView2 = @"C:\Users\sergiopedri\.nuget\packages\microsoft.web.webview2\1.0.3179.45\lib\Microsoft.Web.WebView2.Core.winmd";
        const string SdkUnionMetadata = @"C:\Program Files (x86)\Windows Kits\10\UnionMetadata\10.0.26100.0\Windows.winmd";

        if (Directory.Exists(output))
        {
            Directory.Delete(output, true);
        }
        _ = Directory.CreateDirectory(output);

        // Mirrors '-include AuthoringTest.*' from the truth .rsp
        string[] includes = new[]
        {
            "AuthoringTest.BasicEnum",
            "AuthoringTest.FlagsEnum",
            "AuthoringTest.BasicDelegate",
            "AuthoringTest.ComplexDelegate",
            "AuthoringTest.DoubleDelegate",
            "AuthoringTest.BasicClass",
            "AuthoringTest.CustomWWW",
            "AuthoringTest.BasicStruct",
            "AuthoringTest.ComplexStruct",
            "AuthoringTest.IBasicClassClass",
            "AuthoringTest.CustomProperty",
            "AuthoringTest.CustomPropertyStructType",
            "AuthoringTest.ICustomPropertyClass",
            "AuthoringTest.CustomPropertyRecordTypeFactory",
            "AuthoringTest.ICustomPropertyRecordTypeFactoryStatic",
            "AuthoringTest.ICustomPropertyRecordTypeFactoryClass",
            "AuthoringTest.CustomPropertyProviderWithExplicitImplementation",
            "AuthoringTest.CustomPropertyWithExplicitImplementation",
            "AuthoringTest.IDouble",
            "AuthoringTest.IAnotherInterface",
            "AuthoringTest.TestClass",
            "AuthoringTest.CustomDictionary",
            "AuthoringTest.DisposableClass",
            "AuthoringTest.IDisposableClassClass",
            "AuthoringTest.ITestClassStatic",
            "AuthoringTest.ITestClassFactory",
            "AuthoringTest.ITestClassClass",
            "AuthoringTest.CustomReadOnlyDictionary",
            "AuthoringTest.ICustomReadOnlyDictionaryFactory",
            "AuthoringTest.CustomVector",
            "AuthoringTest.ICustomVectorFactory",
            "AuthoringTest.CustomVectorView",
            "AuthoringTest.ICustomVectorViewFactory",
            "AuthoringTest.CustomVector2",
            "AuthoringTest.ICustomVector2Factory",
            "AuthoringTest.StaticClass",
            "AuthoringTest.IStaticClassStatic",
            "AuthoringTest.IStaticClassClass",
            "AuthoringTest.ButtonUtils",
            "AuthoringTest.CustomButton",
            "AuthoringTest.ICustomButtonFactory",
            "AuthoringTest.ICustomButtonClass",
            "AuthoringTest.IButtonUtilsStatic",
            "AuthoringTest.IButtonUtilsClass",
            "AuthoringTest.CustomStackPanel",
            "AuthoringTest.ICustomStackPanelClass",
            "AuthoringTest.CustomXamlServiceProvider",
            "AuthoringTest.CustomNotifyPropertyChanged",
            "AuthoringTest.CustomNotifyPropertyChangedAndChanging",
            "AuthoringTest.CustomCommand",
            "AuthoringTest.ICustomCommandClass",
            "AuthoringTest.CustomNotifyCollectionChanged",
            "AuthoringTest.CustomNotifyDataErrorInfo",
            "AuthoringTest.CustomNotifyDataErrorInfo2",
            "AuthoringTest.CustomEnumerable",
            "AuthoringTest.ICustomEnumerableFactory",
            "AuthoringTest.CustomXamlMetadataProvider",
            "AuthoringTest.SingleInterfaceClass",
            "AuthoringTest.IDouble2",
            "AuthoringTest.ExplicltlyImplementedClass",
            "AuthoringTest.IExplicltlyImplementedClassClass",
            "AuthoringTest.ObservableVector",
            "AuthoringTest.IInterfaceInheritance",
            "AuthoringTest.InterfaceInheritance",
            "AuthoringTest.MultipleInterfaceMappingClass",
            "AuthoringTest.CustomDictionary2",
            "AuthoringTest.TestCollection",
            "AuthoringTest.ITestCollectionClass",
            "AuthoringTest.IPartialInterface",
            "AuthoringTest.PartialClass",
            "AuthoringTest.PartialStruct",
            "AuthoringTest.IPartialClassFactory",
            "AuthoringTest.IPartialClassClass",
            "AuthoringTest.IPublicInterface",
            "AuthoringTest.ICustomInterfaceGuid",
            "AuthoringTest.CustomInterfaceGuidClass",
            "AuthoringTest.NonActivatableType",
            "AuthoringTest.INonActivatableTypeClass",
            "AuthoringTest.NonActivatableFactory",
            "AuthoringTest.INonActivatableFactoryStatic",
            "AuthoringTest.INonActivatableFactoryClass",
            "AuthoringTest.TypeOnlyActivatableViaItsOwnFactory",
            "AuthoringTest.ITypeOnlyActivatableViaItsOwnFactoryStatic",
            "AuthoringTest.ITypeOnlyActivatableViaItsOwnFactoryClass",
            "AuthoringTest.AnotherNamespace.IOutParams",
            "AuthoringTest.AnotherNamespace.NullableParamClass",
            "AuthoringTest.AnotherNamespace.INullableParamClassClass",
            "AuthoringTest.AnotherNamespace.MappedTypeParamClass",
            "AuthoringTest.AnotherNamespace.IMappedTypeParamClassClass",
            "AuthoringTest.AnotherNamespace.MixedArrayClass",
            "AuthoringTest.AnotherNamespace.IMixedArrayClassClass",
            "AuthoringTest.AnotherNamespace.ICustomResource",
            "AuthoringTest.AnotherNamespace.DisposableResource",
            "AuthoringTest.AnotherNamespace.MultiConstructorClass",
            "AuthoringTest.AnotherNamespace.IMultiConstructorClassFactory",
            "AuthoringTest.AnotherNamespace.IMultiConstructorClassClass",
            "AuthoringTest.AnotherNamespace.StaticComplexProps",
            "AuthoringTest.AnotherNamespace.IStaticComplexPropsStatic",
            "AuthoringTest.AnotherNamespace.IStaticComplexPropsClass",
            "AuthoringTest.AnotherNamespace.OverloadedMethodClass",
            "AuthoringTest.AnotherNamespace.IOverloadedMethodClassStatic",
            "AuthoringTest.AnotherNamespace.IOverloadedMethodClassClass",
            "AuthoringTest.AnotherNamespace.InnerStruct",
            "AuthoringTest.AnotherNamespace.OuterStruct",
            "AuthoringTest.AnotherNamespace.MultiParamDelegate",
            "AuthoringTest.AnotherNamespace.StructParamDelegate",
            "AuthoringTest.AnotherNamespace.StructReturnDelegate",
            "AuthoringTest.AnotherNamespace.DetailedFlags",
            "AuthoringTest.AnotherNamespace.Priority",
            "AuthoringTest.AnotherNamespace.AsyncMethodClass",
            "AuthoringTest.AnotherNamespace.IAsyncMethodClassClass",
            "AuthoringTest.AnotherNamespace.IVersionedInterface",
            "AuthoringTest.AnotherNamespace.DeprecatedMembersClass",
            "AuthoringTest.AnotherNamespace.IDeprecatedMembersClassClass",
            "AuthoringTest.AnotherNamespace.NotifyWithCustomInterface",
            "AuthoringTest.AnotherNamespace.INotifyWithCustomInterfaceClass",
            "AuthoringTest.AnotherNamespace.FactoryAndStaticClass",
            "AuthoringTest.AnotherNamespace.IFactoryAndStaticClassStatic",
            "AuthoringTest.AnotherNamespace.IFactoryAndStaticClassFactory",
            "AuthoringTest.AnotherNamespace.IFactoryAndStaticClassClass",
            "AuthoringTest.AnotherNamespace.IFullFeaturedInterface",
            "AuthoringTest.AnotherNamespace.FullFeaturedClass",
            "AuthoringTest.AnotherNamespace.IFullFeaturedClassClass",
            "AuthoringTest.AnotherNamespace.AnotherNamespaceContract",
            "AuthoringTest.AnotherNamespace.ContractVersionedClass",
            "AuthoringTest.AnotherNamespace.IContractVersionedClassClass",
            "AuthoringTest.AnotherNamespace.ContractVersionedClassV2",
            "AuthoringTest.AnotherNamespace.IContractVersionedClassV2Class",
            "AuthoringTest.AnotherNamespace.IContractVersionedMembersV1",
            "AuthoringTest.AnotherNamespace.IContractVersionedMembersV2",
            "AuthoringTest.AnotherNamespace.ContractVersionedMembersClass",
            "AuthoringTest.AnotherNamespace.IContractVersionedMembersClassClass",
            "AuthoringTest.AnotherNamespace.IVersionedMembersV1",
            "AuthoringTest.AnotherNamespace.IVersionedMembersV2",
            "AuthoringTest.AnotherNamespace.VersionedMembersClass",
            "AuthoringTest.AnotherNamespace.IVersionedMembersClassClass",
        };

        // Inputs: SDK + WindowsAppSDK + WinUI + WebView2 + WindowsRuntime.Internal + AuthoringTest
        string[] inputs = new[]
        {
            SdkUnionMetadata,
            AppSdkInteractive,
            AppSdkWinui,
            AppSdkFoundation,
            WebView2,
            InternalWinmd,
            AuthoringTestWinmd,
        };

        try
        {
            ProjectionWriter.Run(new ProjectionWriterOptions
            {
                InputPaths = inputs,
                OutputFolder = output,
                Include = includes,
                Exclude = new[] { "Windows" },
                Component = true,
            });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }

        Console.WriteLine($"Generated {Directory.GetFiles(output, "*.cs").Length} files in {output}");
        return 0;
    }
}

