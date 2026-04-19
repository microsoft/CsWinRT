// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AsmResolver;
using AsmResolver.DotNet;
using WindowsRuntime.ProjectionGenerator.Errors;

#pragma warning disable IDE0270

namespace WindowsRuntime.ProjectionGenerator.Generation;

/// <inheritdoc cref="ProjectionGenerator"/>
internal partial class ProjectionGenerator
{
    /// <summary>
    /// Creates a temporary folder for CsWinRT generation output.
    /// </summary>
    /// <returns>The path to the created temporary folder.</returns>
    /// <remarks>
    /// The returned folder is guaranteed to already exist when this method returns.
    /// </remarks>
    private static string GetTempFolder()
    {
        // Create a temporary folder with this structure: '%TEMP%/CsWinRT/<RANDOM_NAME>'
        string outputDir = Path.Combine(Path.GetTempPath(), "CsWinRT", Path.GetRandomFileName()).TrimEnd('\\');

        // Ensure the folder does exist (code using it might rely on it existing already)
        _ = Directory.CreateDirectory(outputDir);

        return outputDir;
    }

    /// <summary>
    /// Runs the processing logic for the generator.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <returns>The resulting state.</returns>
    private static ProjectionGeneratorProcessingState ProcessReferences(ProjectionGeneratorArgs args)
    {
        args.Token.ThrowIfCancellationRequested();

        GenerateRspFile(args, out string outputFolder, out string rspFile, out HashSet<string> projectionReferenceAssemblies, out bool hasTypesToProject);

        string[] referencesWithoutProjections = [.. args.ReferenceAssemblyPaths.Where(r => !projectionReferenceAssemblies.Contains(r))];

        return new ProjectionGeneratorProcessingState(outputFolder, rspFile, referencesWithoutProjections, hasTypesToProject);
    }

    /// <summary>
    /// Runs the source generation logic for the generator.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="processingState">The state from the processing phase.</param>
    private static void GenerateSources(ProjectionGeneratorArgs args, ProjectionGeneratorProcessingState processingState)
    {
        ProcessStartInfo processInfo = new()
        {
            FileName = args.CsWinRTExePath,
            Arguments = "@" + processingState.RspFilePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true
        };

        Process? cswinrtProcess;

        try
        {
            cswinrtProcess = Process.Start(processInfo);

            // Make sure we did successfully start the process ('Start' can return 'null')
            if (cswinrtProcess is null)
            {
                throw WellKnownProjectionGeneratorExceptions.CsWinRTProcessStartError();
            }
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw WellKnownProjectionGeneratorExceptions.CsWinRTProcessStartError(e);
        }

        // Validate that generation was successful
        using (cswinrtProcess)
        {
            string error = cswinrtProcess.StandardError.ReadToEnd();

            cswinrtProcess.WaitForExit();

            if (cswinrtProcess.ExitCode != 0)
            {
                throw WellKnownProjectionGeneratorExceptions.CsWinRTProcessError(
                    exitCode: cswinrtProcess.ExitCode,
                    exception: new Win32Exception(cswinrtProcess.ExitCode, error));
            }
        }
    }

    /// <summary>
    /// Generates a response file for CsWinRT based on the provided arguments and reference assemblies.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="outputFolder">The folder where sources will be generated.</param>
    /// <param name="rspFile">The generated response file for running <c>cswinrt.exe</c>.</param>
    /// <param name="projectionReferenceAssemblies">The projection reference assemblies which were used to generate the response file.</param>
    /// <param name="hasTypesToProject">Whether any types were found to include in the projection.</param>
    private static void GenerateRspFile(
        ProjectionGeneratorArgs args,
        out string outputFolder,
        out string rspFile,
        out HashSet<string> projectionReferenceAssemblies,
        out bool hasTypesToProject)
    {
        args.Token.ThrowIfCancellationRequested();

        outputFolder = GetTempFolder();
        rspFile = Path.Combine(outputFolder, "ProjectionGenerator.rsp");
        projectionReferenceAssemblies = [];
        hasTypesToProject = false;

        using StreamWriter fileStream = new(rspFile);

        // Filter out .winmd files from the resolver paths — they use version 255.255
        // which AsmResolver 6.0.0-rc.1 cannot handle when creating a RuntimeContext.
        string[] resolverPaths = [.. args.ReferenceAssemblyPaths
            .Where(p => !p.EndsWith(".winmd", StringComparison.OrdinalIgnoreCase))];

        PathAssemblyResolver resolver = new(resolverPaths);

        bool isWindowsSdkMode = args.WindowsSdkOnly || args.WindowsUIXamlProjection;
        bool isComponentMode = args.AssemblyName == "WinRT.Component";

        // In component mode, read the component .winmd files directly to get the WinRT type
        // includes. The .winmd is the authoritative source of WinRT types and only contains
        // the public WinRT API surface without implementation details like ABI types.
        // We identify component WinMDs by checking if a corresponding assembly with
        // [WindowsRuntimeComponentAssembly] exists in the reference assemblies.
        if (isComponentMode)
        {
            // Collect the names of all component assemblies from the references
            HashSet<string> componentAssemblyNames = [];

            foreach (string refPath in args.ReferenceAssemblyPaths)
            {
                if (refPath.EndsWith(".winmd", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                ModuleDefinition refModule = ModuleDefinition.FromFile(refPath, resolver.ReaderParameters);

                if (IsComponentAssembly(refModule) && refModule.Assembly?.Name is Utf8String name)
                {
                    _ = componentAssemblyNames.Add(name.Value);
                }
            }

            // Scan WinMD files matching component assembly names (e.g. "MyComponent.winmd")
            foreach (string winmdPath in args.WinMDPaths)
            {
                string winmdFileName = Path.GetFileNameWithoutExtension(winmdPath);

                if (!componentAssemblyNames.Contains(winmdFileName))
                {
                    continue;
                }

                ModuleDefinition winmdModule = ModuleDefinition.FromFile(winmdPath, resolver.ReaderParameters, createRuntimeContext: false);

                foreach (TypeDefinition type in winmdModule.TopLevelTypes)
                {
                    if (type.Name?.Value is "<Module>")
                    {
                        continue;
                    }

                    fileStream.WriteLine($"-include {type.FullName}");
                    hasTypesToProject = true;
                }
            }
        }

        // In non-component mode, scan reference assemblies to determine type includes.
        // Component mode handles this above via WinMD scanning.
        if (!isComponentMode)
        {
            foreach (string referenceAssemblyPath in args.ReferenceAssemblyPaths)
            {
                ModuleDefinition moduleDefinition = ModuleDefinition.FromFile(referenceAssemblyPath, resolver.ReaderParameters);

                // For non-component assemblies, check if this is a WinRT reference assembly.
                if (!IsReferenceAssembly(moduleDefinition) || !IsWindowsRuntimeReferenceAssembly(moduleDefinition))
                {
                    continue;
                }

                bool isWindowsSdk = IsWindowsSdkAssembly(moduleDefinition);

                // By default, Windows SDK types are excluded (they go into WinRT.Sdk.Projection.dll
                // and WinRT.Sdk.Xaml.Projection.dll). In WindowsSdkOnly mode where we are generating
                // those dlls is when we will include them.
                if (isWindowsSdk && !isWindowsSdkMode)
                {
                    // Track this as a projection assembly so it's excluded from compilation references.
                    // WinRT.Sdk.Projection.dll and WinRT.Sdk.Xaml.Projection.dll replace these types.
                    _ = projectionReferenceAssemblies.Add(referenceAssemblyPath);
                    continue;
                }

                // Skip other projection binaries we may get.
                if (!isWindowsSdk && isWindowsSdkMode)
                {
                    continue;
                }

                _ = projectionReferenceAssemblies.Add(referenceAssemblyPath);

                if (moduleDefinition.Assembly is not null)
                {
                    if (isWindowsSdk)
                    {
                        // Write the filtes for the Windows SDK projection mode.
                        WriteWindowsSdkFilters(fileStream, args.WindowsUIXamlProjection);
                        hasTypesToProject = true;

                        continue;
                    }
                    else if (moduleDefinition.Assembly.Name == "Microsoft.WinUI")
                    {
                        // In addition to projecting the individual types, make sure
                        // the additions get included by including the namespace.
                        fileStream.WriteLine($"-include Microsoft.UI");
                    }
                }

                foreach (TypeDefinition exportedType in moduleDefinition.TopLevelTypes)
                {
                    fileStream.WriteLine($"-include {exportedType.FullName}");
                    hasTypesToProject = true;
                }
            }
        }

        // In Windows SDK mode, if no Microsoft.Windows.SDK.NET reference assembly was found
        // (e.g., pipeline builds that pass WinMDs directly), hardcode the includes.
        if (isWindowsSdkMode && projectionReferenceAssemblies.Count == 0)
        {
            WriteWindowsSdkFilters(fileStream, args.WindowsUIXamlProjection);
        }

        // If we're not in Windows SDK mode, we exclude the Windows namespace to avoid
        // the merged projection from generating all namespaces when there are no projection references
        // and thereby no includes / excludes passed to cswinrt. 
        if (!isWindowsSdkMode)
        {
            fileStream.WriteLine("-exclude Windows");
        }

        fileStream.WriteLine($"-target {args.TargetFramework}");
        fileStream.WriteLine($"-input {args.WindowsMetadata}");
        fileStream.WriteLine($"-output \"{outputFolder}\"");

        // When generating WinRT.Component.dll, pass -component to cswinrt.exe to enable
        // component-specific code generation (activation factories, exclusive-to interfaces, etc.)
        if (args.AssemblyName == "WinRT.Component")
        {
            fileStream.WriteLine("-component");
        }

        foreach (string winmdPath in args.WinMDPaths)
        {
            fileStream.WriteLine($"-input \"{winmdPath}\"");
        }
    }

    /// <summary>
    /// Writes the cswinrt.exe include/exclude filter directives for the Windows SDK projection.
    /// </summary>
    /// <param name="writer">The RSP file writer.</param>
    /// <param name="xamlProjection">
    /// When <c>true</c>, writes the Windows.UI.Xaml filter set.
    /// When <c>false</c>, writes the base Windows SDK filter set.
    /// </param>
    private static void WriteWindowsSdkFilters(StreamWriter writer, bool xamlProjection)
    {
        if (xamlProjection)
        {
            writer.WriteLine("-exclude Windows");
            writer.WriteLine("-include Windows.UI.Colors");
            writer.WriteLine("-include Windows.UI.ColorHelper");
            writer.WriteLine("-include Windows.UI.IColorHelper");
            writer.WriteLine("-include Windows.UI.IColors");
            writer.WriteLine("-include Windows.UI.Text.FontWeights");
            writer.WriteLine("-include Windows.UI.Text.IFontWeights");
            writer.WriteLine("-include Windows.UI.Xaml");
            writer.WriteLine("-include Windows.ApplicationModel.Store.Preview.WebAuthenticationCoreManagerHelper");
            writer.WriteLine("-include Windows.ApplicationModel.Store.Preview.IWebAuthenticationCoreManagerHelper");
            writer.WriteLine("-exclude Windows.UI.Xaml.Interop");
            writer.WriteLine("-exclude Windows.UI.Xaml.Data.BindableAttribute");
            writer.WriteLine("-exclude Windows.UI.Xaml.Markup.ContentPropertyAttribute");
        }
        else
        {
            writer.WriteLine("-include Windows");
            writer.WriteLine("-include WindowsRuntime.Internal");

            writer.WriteLine("-exclude Windows.UI.Colors");
            writer.WriteLine("-exclude Windows.UI.ColorHelper");
            writer.WriteLine("-exclude Windows.UI.IColorHelper");
            writer.WriteLine("-exclude Windows.UI.IColors");
            writer.WriteLine("-exclude Windows.UI.Text.FontWeights");
            writer.WriteLine("-exclude Windows.UI.Text.IFontWeights");
            writer.WriteLine("-exclude Windows.UI.Xaml");
            writer.WriteLine("-exclude Windows.ApplicationModel.Store.Preview.WebAuthenticationCoreManagerHelper");
            writer.WriteLine("-exclude Windows.ApplicationModel.Store.Preview.IWebAuthenticationCoreManagerHelper");
            writer.WriteLine("-include Windows.UI.Xaml.Interop");
            writer.WriteLine("-include Windows.UI.Xaml.Data.BindableAttribute");
            writer.WriteLine("-include Windows.UI.Xaml.Markup.ContentPropertyAttribute");
        }
    }

    /// <summary>
    /// Checks if the specified module definition represents a reference assembly.
    /// </summary>
    /// <param name="moduleDefinition">The module definition to check.</param>
    /// <returns><c>true</c> if the module is a reference assembly; otherwise, <c>false</c>.</returns>
    private static bool IsReferenceAssembly(ModuleDefinition moduleDefinition)
    {
        return moduleDefinition.Assembly is not null && moduleDefinition.Assembly.HasCustomAttribute("System.Runtime.CompilerServices"u8, "ReferenceAssemblyAttribute"u8);
    }

    /// <summary>
    /// Checks if the specified module definition represents a Windows Runtime reference assembly.
    /// </summary>
    /// <param name="moduleDefinition">The module definition to check.</param>
    /// <returns><c>true</c> if the module is a Windows Runtime reference assembly; otherwise, <c>false</c>.</returns>
    private static bool IsWindowsRuntimeReferenceAssembly(ModuleDefinition moduleDefinition)
    {
        return moduleDefinition.Assembly is not null && moduleDefinition.Assembly.HasCustomAttribute("WindowsRuntime.InteropServices"u8, "WindowsRuntimeReferenceAssemblyAttribute"u8);
    }

    /// <summary>
    /// Checks if the specified module definition represents a Windows SDK assembly
    /// (i.e. <c>Microsoft.Windows.SDK.NET</c> or <c>Microsoft.Windows.UI.Xaml</c>).
    /// </summary>
    /// <param name="moduleDefinition">The module definition to check.</param>
    /// <returns><c>true</c> if the module is a Windows SDK assembly; otherwise, <c>false</c>.</returns>
    private static bool IsWindowsSdkAssembly(ModuleDefinition moduleDefinition)
    {
        return
            (moduleDefinition.Assembly?.Name is Utf8String sdkName && sdkName.AsSpan().SequenceEqual("Microsoft.Windows.SDK.NET"u8)) ||
            (moduleDefinition.Assembly?.Name is Utf8String xamlName && xamlName.AsSpan().SequenceEqual("Microsoft.Windows.UI.Xaml"u8));
    }

    /// <summary>
    /// Checks if the specified module definition represents a Windows Runtime component assembly
    /// (i.e. an assembly annotated with <c>[WindowsRuntimeComponentAssembly]</c>).
    /// </summary>
    /// <param name="moduleDefinition">The module definition to check.</param>
    /// <returns><c>true</c> if the module is a Windows Runtime component assembly; otherwise, <c>false</c>.</returns>
    private static bool IsComponentAssembly(ModuleDefinition moduleDefinition)
    {
        return moduleDefinition.Assembly is not null && moduleDefinition.Assembly.HasCustomAttribute("WindowsRuntime.InteropServices"u8, "WindowsRuntimeComponentAssemblyAttribute"u8);
    }
}