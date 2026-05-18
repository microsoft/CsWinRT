// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using WindowsRuntime.ProjectionWriter;

namespace WindowsRuntime.ProjectionWriter.TestRunner;

internal static class Program
{
    public static int Main(string[] args)
    {
        // Modes:
        //   1) Single .winmd path → simple test (legacy)
        //   2) "compare" <winmd-folder> <internal-winmd> <output-folder> [--ref] → SDK projection mode
        //   3) "compare-xaml" <xaml-winmd-folder> <internal-winmd> <output-folder> [--ref] → XAML projection mode
        //   4) "compare-authoring" <output-folder> → component projection mode (uses fixed paths
        //      that mirror the .rsp file in 'authoring-projection\generated-sources\ProjectionGenerator.rsp'
        //      shipped in the test inputs folder).
        //
        // The optional [--ref] flag enables reference-projection mode (mirrors the C++
        // tool's -reference_projection flag and the CsWinRTGenerateReferenceProjection
        // MSBuild property): emits a public-API-only projection without
        // IWindowsRuntimeInterface<T> markers, ABI helpers, vtables, etc.
        bool refMode = Array.IndexOf(args, "--ref") >= 0;
        if (args.Length >= 4 && args[0] == "compare")
        {
            return RunCompare(args[1], args[2], args[3], refMode);
        }
        if (args.Length >= 4 && args[0] == "compare-xaml")
        {
            return RunCompareXaml(args[1], args[2], args[3], refMode);
        }
        if (args.Length >= 2 && args[0] == "compare-authoring")
        {
            return RunCompareAuthoring(args[1]);
        }
        if (args.Length >= 2 && args[0] == "rsp")
        {
            return RunRsp(args[1], refMode);
        }
        if (args.Length >= 1 && args[0] == "smoke")
        {
            return RunSmoke();
        }

        return RunSimple(args);
    }

    /// <summary>
    /// Reads a `.rsp` file (matching the orchestrator's response file format) and invokes
    /// <see cref="ProjectionWriter.Run"/> with the parsed options. Used by the refactor
    /// validation harness to drive the writer with input-aligned scenarios.
    /// </summary>
    private static int RunRsp(string rspPath, bool refMode)
    {
        if (!File.Exists(rspPath)) { Console.Error.WriteLine($"RSP not found: {rspPath}"); return 1; }
        string text = File.ReadAllText(rspPath);
        var inputs = new System.Collections.Generic.List<string>();
        var include = new System.Collections.Generic.List<string>();
        var exclude = new System.Collections.Generic.List<string>();
        string? outputFolder = null;
        bool component = false, internalMode = false;
        int maxDegreesOfParallelism = -1;
        var tokens = new System.Collections.Generic.List<string>();
        foreach (string raw in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) { continue; }
            int sp = line.IndexOf(' ');
            if (sp < 0) { tokens.Add(line); }
            else { tokens.Add(line.Substring(0, sp)); tokens.Add(line.Substring(sp + 1).Trim()); }
        }
        for (int i = 0; i < tokens.Count; i++)
        {
            string a = tokens[i];
            string? next = i + 1 < tokens.Count ? tokens[i + 1] : null;
            switch (a)
            {
                case "--input-paths": case "--input-path": case "--input":
                    if (next is not null) { inputs.AddRange(next.Split(',', StringSplitOptions.RemoveEmptyEntries)); i++; } break;
                case "--output-directory": case "--output-folder": case "--output":
                    if (next is not null) { outputFolder = next; i++; } break;
                case "--include-namespaces": case "--include":
                    if (next is not null) { include.AddRange(next.Split(',', StringSplitOptions.RemoveEmptyEntries)); i++; } break;
                case "--exclude-namespaces": case "--exclude":
                    if (next is not null) { exclude.AddRange(next.Split(',', StringSplitOptions.RemoveEmptyEntries)); i++; } break;
                case "--component": component = true; break;
                case "--internal": internalMode = true; break;
                case "--reference-projection": refMode = true; break;
                case "--max-degrees-of-parallelism": case "--mdop":
                    if (next is not null && int.TryParse(next, out int mdop)) { maxDegreesOfParallelism = mdop; i++; } break;
                case "--target-framework": if (next is not null) { i++; } break;
            }
        }
        if (outputFolder is null) { Console.Error.WriteLine("Missing --output-directory"); return 1; }
        if (Directory.Exists(outputFolder)) { Directory.Delete(outputFolder, true); }
        _ = Directory.CreateDirectory(outputFolder);
        try
        {
            ProjectionWriter.Run(new ProjectionWriterOptions
            {
                InputPaths = inputs, OutputFolder = outputFolder,
                Include = include, Exclude = exclude,
                Component = component, Internal = internalMode,
                ReferenceProjection = refMode, Verbose = false,
                MaxDegreesOfParallelism = maxDegreesOfParallelism,
            });
        }
        catch (Exception ex) { Console.Error.WriteLine($"ERROR: {ex.Message}"); Console.Error.WriteLine(ex.StackTrace); return 1; }
        Console.WriteLine($"Generated {Directory.GetFiles(outputFolder, "*.cs", SearchOption.AllDirectories).Length} files");
        return 0;
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
    private static int RunCompare(string winmdFolder, string internalWinmd, string output, bool referenceProjection = false)
    {
        if (Directory.Exists(output))
        {
            Directory.Delete(output, true);
        }
        _ = Directory.CreateDirectory(output);

        // Truth uses per-contract WinMD files (one .winmd per ApiContract). When the caller passes
        // the unified UnionMetadata\Windows.winmd, redirect to the per-contract folder so the
        // [WindowsRuntimeMetadata(...)] attribute argument matches truth (= contract file stem).
        string resolvedWinmd = winmdFolder;
        const string PerContractFolder = @"C:\Users\sergiopedri\.nuget\packages\microsoft.windows.sdk.net.ref\10.0.26100.85-preview\winmd\windows";
        if (winmdFolder.EndsWith(@"\Windows.winmd", StringComparison.OrdinalIgnoreCase) && Directory.Exists(PerContractFolder))
        {
            resolvedWinmd = PerContractFolder;
        }

        try
        {
            ProjectionWriter.Run(new ProjectionWriterOptions
            {
                InputPaths = new[] { resolvedWinmd, internalWinmd },
                OutputFolder = output,
                ReferenceProjection = referenceProjection,
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
    private static int RunCompareXaml(string xamlWinmdFolder, string internalWinmd, string output, bool referenceProjection = false)
    {
        if (Directory.Exists(output))
        {
            Directory.Delete(output, true);
        }
        _ = Directory.CreateDirectory(output);

        // Same as compare: prefer per-contract .winmd folder so [WindowsRuntimeMetadata(...)] matches truth.
        // The XAML scenario uses the 'xaml' subfolder which contains a larger Windows.Foundation.UniversalApiContract.winmd
        // that holds the Windows.UI.Xaml.* types as well.
        string resolvedWinmd = xamlWinmdFolder;
        const string PerContractFolder = @"C:\Users\sergiopedri\.nuget\packages\microsoft.windows.sdk.net.ref\10.0.26100.85-preview\winmd\xaml";
        if (xamlWinmdFolder.EndsWith(@"\Windows.winmd", StringComparison.OrdinalIgnoreCase) && Directory.Exists(PerContractFolder))
        {
            resolvedWinmd = PerContractFolder;
        }

        try
        {
            ProjectionWriter.Run(new ProjectionWriterOptions
            {
                InputPaths = new[] { resolvedWinmd, internalWinmd },
                OutputFolder = output,
                ReferenceProjection = referenceProjection,
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

    /// <summary>
    /// Smoke tests for the <c>suppressEmptyLines</c> overload on
    /// <see cref="WindowsRuntime.ProjectionWriter.Writers.IndentedTextWriter"/>'s interpolated
    /// string handler. Validates that:
    /// <list type="bullet">
    /// <item>When all interpolation holes on a template line expand to empty content, the line's
    ///       blank residue is suppressed (no stray newline emitted).</item>
    /// <item>When at least one hole on a line emits content, no suppression occurs.</item>
    /// <item>Literal blank lines in the template (consecutive newlines not separated by a hole)
    ///       are always preserved.</item>
    /// <item>The legacy overload (without <c>suppressEmptyLines</c>) behaves identically to before.</item>
    /// </list>
    /// </summary>
    private static int RunSmoke()
    {
        int failures = 0;

        WindowsRuntime.ProjectionWriter.Writers.IndentedTextWriter Make() =>
            new WindowsRuntime.ProjectionWriter.Writers.IndentedTextWriter();

        // Test cases: each is (description, action_producing_string, expected_string).
        var tests = new System.Collections.Generic.List<(string desc, Func<string> actual, string expected)>
        {
            // Case 1: all 3 attribute holes empty → only header remains.
            ("S1: all-empty middle holes collapse to nothing",
                () => {
                    var w = Make();
                    string a = string.Empty, b = string.Empty, c = string.Empty;
                    w.WriteLine(isMultiline: true, $$"""
                        [Header]
                        {{a}}
                        {{b}}
                        {{c}}
                        public class Foo
                        """);
                    return w.ToString();
                },
                "[Header]\npublic class Foo\n"),

            // Case 2: first hole non-empty, others empty → first kept, others collapsed.
            ("S2: partial-empty holes collapse only the empty ones",
                () => {
                    var w = Make();
                    string a = "[A]", b = string.Empty, c = string.Empty;
                    w.WriteLine(isMultiline: true, $$"""
                        [Header]
                        {{a}}
                        {{b}}
                        {{c}}
                        public class Foo
                        """);
                    return w.ToString();
                },
                "[Header]\n[A]\npublic class Foo\n"),

            // Case 3: all holes non-empty → every line preserved.
            ("S3: all-non-empty holes preserved",
                () => {
                    var w = Make();
                    string a = "[A]", b = "[B]", c = "[C]";
                    w.WriteLine(isMultiline: true, $$"""
                        [Header]
                        {{a}}
                        {{b}}
                        {{c}}
                        public class Foo
                        """);
                    return w.ToString();
                },
                "[Header]\n[A]\n[B]\n[C]\npublic class Foo\n"),

            // Case 4: literal blank line preserved (no hole between newlines).
            ("S4: literal blank line is preserved (no hole between newlines)",
                () => {
                    var w = Make();
                    string a = "[A]";
                    w.WriteLine(isMultiline: true, $$"""
                        Line1

                        {{a}}
                        Line3
                        """);
                    return w.ToString();
                },
                "Line1\n\n[A]\nLine3\n"),

            // Case 5: multiple empty holes on a single line → still collapsed.
            ("S5: multiple empty holes on one line still collapse",
                () => {
                    var w = Make();
                    string a = string.Empty, b = string.Empty;
                    w.WriteLine(isMultiline: true, $$"""
                        [Header]
                        {{a}}{{b}}
                        public class Foo
                        """);
                    return w.ToString();
                },
                "[Header]\npublic class Foo\n"),

            // Case 6: hole at start of template, empty - buffer is empty so suppress rule doesn't fire.
            ("S6: leading empty hole produces a leading blank line (buffer is empty)",
                () => {
                    var w = Make();
                    string a = string.Empty;
                    w.WriteLine(isMultiline: true, $$"""
                        {{a}}
                        public class Foo
                        """);
                    return w.ToString();
                },
                "\npublic class Foo\n"),

            // Case 7: Write (not WriteLine) - no trailing newline appended by the call.
            ("S7: Write (no trailing newline) with all-empty interior holes",
                () => {
                    var w = Make();
                    string a = string.Empty, b = string.Empty;
                    w.Write(isMultiline: true, $$"""
                        [Header]
                        {{a}}
                        {{b}}
                        public class Foo
                        """);
                    return w.ToString();
                },
                "[Header]\npublic class Foo"),

            // Case 8: indentation still applied to non-empty lines.
            ("S8: indented writer with mixed empty/non-empty holes",
                () => {
                    var w = Make();
                    w.IncreaseIndent();
                    string a = string.Empty, b = "[B]";
                    w.WriteLine(isMultiline: true, $$"""
                        [Header]
                        {{a}}
                        {{b}}
                        public class Foo
                        """);
                    return w.ToString();
                },
                "    [Header]\n    [B]\n    public class Foo\n"),

            // Case 9: literal-only multiline template (no holes) is unaffected.
            ("S9: literal-only template (no holes) is unaffected",
                () => {
                    var w = Make();
                    w.WriteLine(isMultiline: true, $$"""
                        Line1
                        Line2

                        Line4
                        """);
                    return w.ToString();
                },
                "Line1\nLine2\n\nLine4\n"),

            // Case 10: callback-style interpolation: empty callback hole is collapsed.
            ("S10: empty string callback collapsed identically to empty string hole",
                () => {
                    var w = Make();
                    string a = "[A]", b = string.Empty;
                    w.WriteLine(isMultiline: true, $$"""
                        [Header]
                        {{a}}
                        {{b}}
                        public class Foo
                        """);
                    return w.ToString();
                },
                "[Header]\n[A]\npublic class Foo\n"),
        };

        foreach ((string desc, Func<string> actualFn, string expected) in tests)
        {
            string actual = actualFn();
            bool pass = actual == expected;
            if (!pass)
            {
                failures++;
            }

            Console.WriteLine($"{(pass ? "PASS" : "FAIL")}: {desc}");

            if (!pass)
            {
                Console.WriteLine($"  expected: {Escape(expected)}");
                Console.WriteLine($"  actual:   {Escape(actual)}");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"=== {tests.Count - failures}/{tests.Count} smoke tests passed ===");

        return failures == 0 ? 0 : 1;

        static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    }
}

