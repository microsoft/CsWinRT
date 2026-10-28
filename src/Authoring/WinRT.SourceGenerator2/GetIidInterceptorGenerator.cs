// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;
using WindowsRuntime.SourceGenerator.Models;

namespace WindowsRuntime.SourceGenerator;

/// <summary>
/// Replaces statically computable <c>WindowsRuntimeMarshal.GetIid&lt;T&gt;()</c> calls with embedded IIDs.
/// </summary>
[Generator]
public sealed partial class GetIidInterceptorGenerator : IIncrementalGenerator
{
    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Analyze candidate calls without retaining compiler objects in the pipeline
        IncrementalValuesProvider<GetIidCallInfo> calls =
            context.SyntaxProvider.CreateSyntaxProvider(
                predicate: Execute.IsTargetNodeValid,
                transform: Execute.GetCallInfo)
            .SkipNullValues();

        // Select the IID for the project's XAML mode
        IncrementalValueProvider<bool> useWindowsUIXamlProjections =
            context.AnalyzerConfigOptionsProvider.Select(Execute.GetUseWindowsUIXamlProjections);

        IncrementalValuesProvider<GetIidInterceptorInfo> interceptors = calls
            .Combine(useWindowsUIXamlProjections)
            .Select(Execute.GetInterceptorInfo)
            .SkipNullValues()
            .WithTrackingName("GetIidInterceptorInfo");

        // Collect value-equatable inputs for emission
        IncrementalValueProvider<EquatableArray<GetIidInterceptorInfo>> collected = interceptors
            .Collect()
            .Select(Execute.GetEquatableInterceptorInfo);

        context.RegisterImplementationSourceOutput(collected, Emit.WriteInterceptors);
    }
}
