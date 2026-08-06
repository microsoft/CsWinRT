// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.Generator.Errors;
using WindowsRuntime.Generator.Helpers;
using WindowsRuntime.ProjectionGenerator.Errors;
using WindowsRuntime.ProjectionWriter;

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

        BuildWriterOptions(
            args,
            out string outputFolder,
            out HashSet<string> projectionReferenceAssemblies,
            out bool hasTypesToProject,
            out ProjectionWriterOptions writerOptions,
            out List<string> componentAssemblyNames,
            out List<string> activationFactoryAssemblyNames);

        string[] referencesWithoutProjections = [.. args.ReferenceAssemblyPaths.Where(r => !projectionReferenceAssemblies.Contains(r))];

        return new ProjectionGeneratorProcessingState(
            outputFolder,
            referencesWithoutProjections,
            writerOptions,
            hasTypesToProject,
            componentAssemblyNames,
            activationFactoryAssemblyNames);
    }

    /// <summary>
    /// Runs the source generation logic for the generator.
    /// </summary>
    /// <param name="processingState">The state from the processing phase.</param>
    private static void GenerateSources(ProjectionGeneratorProcessingState processingState)
    {
        // Invoke the projection writer in-process via its public C# API.
        try
        {
            ProjectionWriter.ProjectionWriter.Run(processingState.WriterOptions);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw WellKnownProjectionGeneratorExceptions.CsWinRTProcessError(exitCode: -1, exception: e);
        }
    }

    /// <summary>
    /// Builds the <see cref="ProjectionWriterOptions"/> from the supplied arguments and reference assemblies.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="outputFolder">The folder where sources will be generated.</param>
    /// <param name="projectionReferenceAssemblies">The projection reference assemblies which were used.</param>
    /// <param name="hasTypesToProject">Whether any types were found to include in the projection.</param>
    /// <param name="writerOptions">The resulting writer options.</param>
    /// <param name="componentAssemblyNames">Sorted simple names of input <c>[WindowsRuntimeComponentAssembly]</c> references (populated only in component mode).</param>
    /// <param name="activationFactoryAssemblyNames">Sorted simple names of input references that only contribute an activation entry point (populated only in component mode).</param>
    private static void BuildWriterOptions(
        ProjectionGeneratorArgs args,
        out string outputFolder,
        out HashSet<string> projectionReferenceAssemblies,
        out bool hasTypesToProject,
        out ProjectionWriterOptions writerOptions,
        out List<string> componentAssemblyNames,
        out List<string> activationFactoryAssemblyNames)
    {
        args.Token.ThrowIfCancellationRequested();

        outputFolder = GetTempFolder();
        projectionReferenceAssemblies = [];
        hasTypesToProject = false;
        componentAssemblyNames = [];
        activationFactoryAssemblyNames = [];

        List<string> includes = [];
        List<string> excludes = [];
        List<string> implementableTypes = [];
        List<string> winmdInputs = [];

        // Paths to the managed implementation assemblies of the component(s) being projected, scanned
        // by the projection writer for implementation details (e.g. the static fields backing XAML
        // dependency properties) that are not present in the .winmd metadata
        List<string> componentImplementationAssemblies = [];

        // Filter out .winmd files from the resolver paths
        string[] resolverPaths = [.. args.ReferenceAssemblyPaths
            .Where(p => !p.EndsWith(".winmd", StringComparison.OrdinalIgnoreCase))];

        PathAssemblyResolver resolver = new(resolverPaths);

        bool isComponentMode = args.AssemblyName == "WinRT.Component";

        // 'WindowsUIXamlProjection' also flows to component mode (for the '[TypeMapAssemblyTarget]' union),
        // but must not put it into Windows SDK mode: the SDK 'Windows.UI.Xaml' namespace comes from
        // 'WinRT.Sdk.Xaml.Projection.dll', so component mode is never Windows SDK mode.
        bool isWindowsSdkMode = (args.WindowsSdkOnly || args.WindowsUIXamlProjection) && !isComponentMode;

        // In component mode, read the component .winmd files directly to get the WinRT type
        // includes. The .winmd is the authoritative source of WinRT types and only contains
        // the public WinRT API surface without implementation details like ABI types.
        // We identify component WinMDs by checking if a corresponding assembly with
        // [WindowsRuntimeComponentAssembly] exists in the reference assemblies.
        if (isComponentMode)
        {
            // Collect the names of all component assemblies from the references
            HashSet<string> componentAssemblyNameSet = [];

            // Assemblies that only contribute an activation entry point to the merged activation chain. These are
            // tracked separately from the component ones: they have no '.winmd' of their own, so they must not
            // drive the type scanning below (a same-named '.winmd' would otherwise be projected a second time).
            HashSet<string> activationFactoryAssemblyNameSet = [];

            foreach (string refPath in args.ReferenceAssemblyPaths)
            {
                if (refPath.EndsWith(".winmd", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                ModuleDefinition refModule = ModuleDefinition.FromFile(refPath, resolver.ReaderParameters);

                if (IsComponentAssembly(refModule))
                {
                    // Keep the path so the projection writer can inspect the managed implementation
                    componentImplementationAssemblies.Add(refPath);

                    if (refModule.Assembly?.Name is Utf8String name)
                    {
                        _ = componentAssemblyNameSet.Add(name.Value);
                    }
                }
                else if (IsActivationFactoryAssembly(refModule) && refModule.Assembly?.Name is Utf8String activationName)
                {
                    _ = activationFactoryAssemblyNameSet.Add(activationName.Value);
                }
            }

            // Sort for stable codegen output
            componentAssemblyNames = [.. componentAssemblyNameSet];
            componentAssemblyNames.Sort(StringComparer.Ordinal);

            activationFactoryAssemblyNames = [.. activationFactoryAssemblyNameSet];
            activationFactoryAssemblyNames.Sort(StringComparer.Ordinal);

            // Scan WinMD files matching component assembly names (e.g. 'MyComponent.winmd')
            foreach (string winmdPath in args.WinMDPaths)
            {
                string winmdFileName = Path.GetFileNameWithoutExtension(winmdPath);

                if (!componentAssemblyNameSet.Contains(winmdFileName))
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

                    includes.Add(type.FullName);
                    hasTypesToProject = true;
                }
            }
        }

        // In non-component mode, scan reference assemblies to determine type includes. Component mode
        // handles this above via .winmd scanning, and the authoring mode is scoped exclusively to the
        // explicitly requested types (everything else already lives in the real projection).
        if (!isComponentMode)
        {
            foreach (string referenceAssemblyPath in args.ReferenceAssemblyPaths)
            {
                ModuleDefinition moduleDefinition = ModuleDefinition.FromFile(referenceAssemblyPath, resolver.ReaderParameters);

                // For non-component assemblies, check if this is a Windows Runtime reference assembly.
                if (!IsReferenceAssembly(moduleDefinition) || !IsWindowsRuntimeReferenceAssembly(moduleDefinition))
                {
                    continue;
                }

                bool isWindowsSdk = IsWindowsSdkAssembly(moduleDefinition);

                // By default, Windows SDK types are excluded (they go into 'WinRT.Sdk.Projection.dll'
                // and 'WinRT.Sdk.Xaml.Projection.dll'). In 'WindowsSdkOnly' mode where we are generating
                // those .dll-s is when we will include them.
                if (isWindowsSdk && !isWindowsSdkMode)
                {
                    // Track this as a projection assembly so it's excluded from compilation references.
                    // 'WinRT.Sdk.Projection.dll' and 'WinRT.Sdk.Xaml.Projection.dll' replace these types.
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
                        // Write the filters for the Windows SDK projection mode.
                        WriteWindowsSdkFilters(includes, excludes, args.WindowsUIXamlProjection);

                        hasTypesToProject = true;

                        continue;
                    }
                    else if (moduleDefinition.Assembly.Name == "Microsoft.WinUI")
                    {
                        // In addition to projecting the individual types, make sure
                        // the additions get included by including the namespace.
                        includes.Add("Microsoft.UI");
                    }
                }

                foreach (TypeDefinition exportedType in moduleDefinition.TopLevelTypes)
                {
                    includes.Add(exportedType.FullName);
                    hasTypesToProject = true;

                    // An authoring reference assembly carries abstract 'ABI.<Ns>.<Class>' base classes with
                    // no implementation. Record the runtime class each one stands for, so the projection
                    // being generated here supplies their real implementation. This is the same contract as
                    // any other reference projection: metadata at compile time, implementation at publish.
                    // The class is read from the marker attribute rather than the base class name: a statics
                    // only runtime class has no instance base, so only its factory base carries the name.
                    if (TryGetImplementableRuntimeClassName(exportedType, out string? runtimeClassName))
                    {
                        implementableTypes.Add(runtimeClassName);
                    }
                }
            }
        }

        // In Windows SDK mode, if no Microsoft.Windows.SDK.NET reference assembly was found
        // (e.g., pipeline builds that pass WinMDs directly), hardcode the includes.
        if (isWindowsSdkMode && projectionReferenceAssemblies.Count == 0)
        {
            WriteWindowsSdkFilters(includes, excludes, args.WindowsUIXamlProjection);

            hasTypesToProject = true;
        }

        // If we're not in Windows SDK mode, we exclude the Windows namespace to avoid
        // the merged projection from generating all namespaces when there are no projection references
        // and thereby no includes / excludes passed to the writer.
        if (!isWindowsSdkMode)
        {
            excludes.Add("Windows");
        }

        // Expand the windows metadata token (path | "local" | "sdk[+]" | version[+]) into
        // actual .winmd file paths (or directories the writer will recursively scan).
        winmdInputs.AddRange(WindowsMetadataExpander.Expand<WellKnownProjectionGeneratorExceptions>(args.WindowsMetadata));

        // When generating 'WinRT.Component.dll', enable component-specific code generation
        // (activation factories, exclusive-to interfaces, etc.).
        bool componentMode = args.AssemblyName == "WinRT.Component";

        foreach (string winmdPath in args.WinMDPaths)
        {
            winmdInputs.Add(winmdPath);
        }

        writerOptions = new ProjectionWriterOptions
        {
            InputPaths = winmdInputs,
            OutputFolder = outputFolder,
            Include = includes,
            Exclude = excludes,
            Component = componentMode,
            ComponentImplementationAssemblyPaths = componentImplementationAssemblies,
            ImplementableTypes = implementableTypes,
            MaxDegreesOfParallelism = args.MaxDegreesOfParallelism,
            CancellationToken = args.Token,
        };
    }

    /// <summary>
    /// Adds the include/exclude filter directives for the Windows SDK projection.
    /// </summary>
    /// <param name="includes">The list of namespace prefixes to include.</param>
    /// <param name="excludes">The list of namespace prefixes to exclude.</param>
    /// <param name="xamlProjection">
    /// When <c>true</c>, writes the Windows.UI.Xaml filter set.
    /// When <c>false</c>, writes the base Windows SDK filter set.
    /// </param>
    private static void WriteWindowsSdkFilters(List<string> includes, List<string> excludes, bool xamlProjection)
    {
        void Include(string ns) => includes.Add(ns);

        void Exclude(string ns) => excludes.Add(ns);

        if (xamlProjection)
        {
            Exclude("Windows");
            Include("Windows.UI.Colors");
            Include("Windows.UI.ColorHelper");
            Include("Windows.UI.IColorHelper");
            Include("Windows.UI.IColors");
            Include("Windows.UI.Text.FontWeights");
            Include("Windows.UI.Text.IFontWeights");
            Include("Windows.UI.Xaml");
            Include("Windows.ApplicationModel.Store.Preview.WebAuthenticationCoreManagerHelper");
            Include("Windows.ApplicationModel.Store.Preview.IWebAuthenticationCoreManagerHelper");
            Exclude("Windows.UI.Xaml.Interop");
            Exclude("Windows.UI.Xaml.Data.BindableAttribute");
            Exclude("Windows.UI.Xaml.Markup.ContentPropertyAttribute");
        }
        else
        {
            Include("Windows");
            Include("WindowsRuntime.Internal");

            Exclude("Windows.UI.Colors");
            Exclude("Windows.UI.ColorHelper");
            Exclude("Windows.UI.IColorHelper");
            Exclude("Windows.UI.IColors");
            Exclude("Windows.UI.Text.FontWeights");
            Exclude("Windows.UI.Text.IFontWeights");
            Exclude("Windows.UI.Xaml");
            Exclude("Windows.ApplicationModel.Store.Preview.WebAuthenticationCoreManagerHelper");
            Exclude("Windows.ApplicationModel.Store.Preview.IWebAuthenticationCoreManagerHelper");
            Include("Windows.UI.Xaml.Interop");
            Include("Windows.UI.Xaml.Data.BindableAttribute");
            Include("Windows.UI.Xaml.Markup.ContentPropertyAttribute");
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

    /// <summary>
    /// Tries to read the Windows Runtime class that a generated abstract base class stands for, from the
    /// <c>[WindowsRuntimeImplementableClass]</c> or <c>[WindowsRuntimeImplementableClassFactory]</c> marker.
    /// </summary>
    /// <param name="type">The exported type to inspect.</param>
    /// <param name="runtimeClassName">The full name of the Windows Runtime class, if the marker is present.</param>
    /// <returns><c>true</c> if <paramref name="type"/> is a generated abstract base class; otherwise, <c>false</c>.</returns>
    private static bool TryGetImplementableRuntimeClassName(TypeDefinition type, [NotNullWhen(true)] out string? runtimeClassName)
    {
        foreach (CustomAttribute attribute in type.CustomAttributes)
        {
            if (attribute.Constructor?.DeclaringType is not { Namespace.Value: "WindowsRuntime" } attributeType ||
                attributeType.Name?.Value is not ("WindowsRuntimeImplementableClassAttribute" or "WindowsRuntimeImplementableClassFactoryAttribute"))
            {
                continue;
            }

            if (attribute.Signature is { FixedArguments: [{ Element: TypeSignature classType }] })
            {
                runtimeClassName = classType.FullName;

                return true;
            }
        }

        runtimeClassName = null;

        return false;
    }

    /// <summary>
    /// Checks if the specified module definition represents an assembly exposing a managed activation entry point
    /// (i.e. an assembly annotated with <c>[WindowsRuntimeComponentAssemblyExportsType]</c>).
    /// </summary>
    /// <param name="moduleDefinition">The module definition to check.</param>
    /// <returns><c>true</c> if the module exposes an activation entry point; otherwise, <c>false</c>.</returns>
    private static bool IsActivationFactoryAssembly(ModuleDefinition moduleDefinition)
    {
        return moduleDefinition.Assembly is not null && moduleDefinition.Assembly.HasCustomAttribute("WindowsRuntime.InteropServices"u8, "WindowsRuntimeComponentAssemblyExportsTypeAttribute"u8);
    }
}
