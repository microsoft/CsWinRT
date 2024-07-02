// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

#nullable enable

namespace WinRT.SourceGenerator;

/// <summary>
/// A diagnostic analyzer that emits errors when 'UseUwp' and 'UseWinUI' are used incorrectly.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp), Shared]
public sealed class WindowsUIXamlProjectionsAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        WinRTRules.MissingUseUwpWhenUsingWindowsUIXamlProjections,
        WinRTRules.UsingUseUwpWhenUsingWinAppSDK,
        WinRTRules.UsingUseWinUIWhenUsingWindowsUIXamlProjections,
        WinRTRules.UsingUseUwpAndUseWinUITogether);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();

        context.RegisterCompilationAction(static context =>
        {
            // Are we referencing the Windows.UI.Xaml projections?
            bool isWindowsUIXamlProjectionsAssemblyReferenced = IsAssemblyReferenced(
                context.Compilation,
                "Microsoft.Windows.UI.Xaml",
                context.CancellationToken);

            // Are we referencing WindowsAppSDK?
            bool isWindowsAppSDKReferenced = IsAssemblyReferenced(
                context.Compilation,
                "Microsoft.Windows.ApplicationModel.WindowsAppRuntime.Projection",
                context.CancellationToken);

            // Check the MSBuild properties
            bool useUwp = context.Options.AnalyzerConfigOptionsProvider.GetUseUwp();
            bool useWinUI = context.Options.AnalyzerConfigOptionsProvider.GetUseWinUI();

            // Error #1: missing 'UseUwp' when using Windows.UI.Xaml projections
            if (!useUwp && isWindowsUIXamlProjectionsAssemblyReferenced)
            {
                context.ReportDiagnostic(Diagnostic.Create(WinRTRules.MissingUseUwpWhenUsingWindowsUIXamlProjections, location: null));
            }

            // Error #2: using 'UseUwp' when referencing WindowsAppSDK
            if (useUwp && isWindowsAppSDKReferenced)
            {
                context.ReportDiagnostic(Diagnostic.Create(WinRTRules.UsingUseUwpWhenUsingWinAppSDK, location: null));
            }

            // Error #3: using 'UseWinUI' when referencing Windows.UI.Xaml projections
            if (useUwp && isWindowsAppSDKReferenced)
            {
                context.ReportDiagnostic(Diagnostic.Create(WinRTRules.UsingUseWinUIWhenUsingWindowsUIXamlProjections, location: null));
            }

            // Error #4: using 'UseUwp' and 'UseWinUI' together
            if (useUwp && isWindowsAppSDKReferenced)
            {
                context.ReportDiagnostic(Diagnostic.Create(WinRTRules.UsingUseUwpAndUseWinUITogether, location: null));
            }
        });
    }

    /// <summary>
    /// Checks whether a given assembly is referenced.
    /// </summary>
    /// <param name="compilation">The input <see cref="Compilation"/> to check.</param>
    /// <param name="assemblyName">The name of the assembly to check.</param>
    /// <param name="token">The cancellation token for the operation.</param>
    /// <returns>Whether <paramref name="compilation"/> references an assembly with a given name.</returns>
    private static bool IsAssemblyReferenced(
        Compilation compilation,
        string assemblyName,
        CancellationToken token)
    {
        foreach (MetadataReference metadataReference in compilation.References)
        {
            token.ThrowIfCancellationRequested();

            // We're only looking for PE files, not project references
            if (metadataReference is not PortableExecutableReference)
            {
                continue;
            }

            // If we can't resolve the assembly symbol, ignore the reference
            if (compilation.GetAssemblyOrModuleSymbol(metadataReference) is not IAssemblySymbol assemblySymbol)
            {
                continue;
            }

            // If we found the target assembly (including in transitive dependencies), we can stop
            if (string.Equals(assemblySymbol.Name, assemblyName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
