// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using WindowsRuntime.SourceGenerator.Models;

namespace WindowsRuntime.SourceGenerator;

/// <inheritdoc cref="GetIidInterceptorGenerator"/>
public partial class GetIidInterceptorGenerator
{
    /// <summary>
    /// Analysis methods for <see cref="GetIidInterceptorGenerator"/>.
    /// </summary>
    private static class Execute
    {
        /// <summary>
        /// Checks whether a syntax node is a candidate IID lookup invocation.
        /// </summary>
        /// <param name="node">The target node.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>Whether the node could call <c>WindowsRuntimeMarshal.GetIid&lt;T&gt;()</c>.</returns>
        [SuppressMessage("Style", "IDE0060", Justification = "The cancellation token is supplied by Roslyn.")]
        public static bool IsTargetNodeValid(SyntaxNode node, CancellationToken token)
        {
            return node is InvocationExpressionSyntax { ArgumentList.Arguments.Count: 0 } invocation &&
                (invocation.Expression is SimpleNameSyntax { Identifier.ValueText: "GetIid" } ||
                 invocation.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "GetIid" });
        }

        /// <summary>
        /// Analyzes a candidate call and releases all compiler objects before incremental caching.
        /// </summary>
        /// <param name="context">The syntax context.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>Its precomputed IIDs and compiler-provided location, when interceptable.</returns>
        public static GetIidCallInfo? GetCallInfo(GeneratorSyntaxContext context, CancellationToken token)
        {
            InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)context.Node;
            SemanticModel model = context.SemanticModel;

            if (model.GetSymbolInfo(invocation, token).Symbol is not IMethodSymbol
                {
                    IsStatic: true,
                    Arity: 1,
                    Parameters.Length: 0,
                    ReturnsByRef: false,
                    ReturnsByRefReadonly: false
                } method ||
                method.ContainingAssembly.Identity.Name != "WinRT.Runtime" ||
                method.ContainingType is not { Name: "WindowsRuntimeMarshal", ContainingType: null } ||
                method.ContainingNamespace.ToDisplayString() != "WindowsRuntime.InteropServices" ||
                !SymbolEqualityComparer.Default.Equals(method.ReturnType, model.Compilation.GetTypeByMetadataName("System.Guid")) ||
                IsInExpressionTree(model.GetOperation(invocation, token)))
            {
                return null;
            }

            Guid? winUIIid = WindowsRuntimeIidResolver.TryGetIid(method.TypeArguments[0], model.Compilation, false, token, out Guid first) ? first : null;
            Guid? uwpIid = WindowsRuntimeIidResolver.TryGetIid(method.TypeArguments[0], model.Compilation, true, token, out Guid second) ? second : null;

            // A reusable library cannot bake in a mapping selected by its eventual publishing application's XAML mode
            if ((winUIIid is null && uwpIid is null) ||
                (model.Compilation.Options.OutputKind == OutputKind.DynamicallyLinkedLibrary && winUIIid != uwpIid))
            {
                return null;
            }

#pragma warning disable RSEXPERIMENTAL002
            InterceptableLocation? location = model.GetInterceptableLocation(invocation, token);

            return location is null ? null : new GetIidCallInfo(winUIIid, uwpIid, location.Version, location.Data);
#pragma warning restore RSEXPERIMENTAL002
        }

        /// <summary>
        /// Gets the project's XAML projection mode.
        /// </summary>
        /// <param name="options">The analyzer configuration.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>Whether the project uses UWP XAML mappings.</returns>
        [SuppressMessage("Style", "IDE0060", Justification = "The cancellation token is supplied by Roslyn.")]
        public static bool GetUseWindowsUIXamlProjections(AnalyzerConfigOptionsProvider options, CancellationToken token)
        {
            return options.GlobalOptions.GetCsWinRTUseWindowsUIXamlProjections();
        }

        /// <summary>
        /// Selects the interceptor IID for the project's XAML mode.
        /// </summary>
        /// <param name="data">The analyzed call and selected XAML mode.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>The interceptor input, if the selected IID is available.</returns>
        [SuppressMessage("Style", "IDE0060", Justification = "The cancellation token is supplied by Roslyn.")]
        public static GetIidInterceptorInfo? GetInterceptorInfo((GetIidCallInfo Call, bool UseWindowsUIXamlProjections) data, CancellationToken token)
        {
            GetIidCallInfo call = data.Call;
            Guid? iid = data.UseWindowsUIXamlProjections ? call.UwpIid : call.WinUIIid;

            return iid is Guid value ? new GetIidInterceptorInfo(value, call.Version, call.Data) : null;
        }

        /// <summary>
        /// Wraps collected interceptor inputs with value equality.
        /// </summary>
        /// <param name="items">The collected inputs.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>The value-equatable inputs.</returns>
        [SuppressMessage("Style", "IDE0060", Justification = "The cancellation token is supplied by Roslyn.")]
        public static EquatableArray<GetIidInterceptorInfo> GetEquatableInterceptorInfo(ImmutableArray<GetIidInterceptorInfo> items, CancellationToken token)
        {
            return items.AsEquatableArray();
        }

        /// <summary>
        /// Checks both explicit lambdas and compiler-translated query lambdas for expression-tree conversion.
        /// </summary>
        /// <param name="operation">The invocation operation.</param>
        /// <returns>Whether this call occurs within an expression tree.</returns>
        private static bool IsInExpressionTree(IOperation? operation)
        {
            for (; operation is not null; operation = operation.Parent)
            {
                if (operation is IConversionOperation
                    {
                        Type: INamedTypeSymbol
                        {
                            OriginalDefinition.MetadataName: "Expression`1",
                            ContainingNamespace: { Name: "Expressions", ContainingNamespace.Name: "Linq" }
                        }
                    })
                {
                    return true;
                }
            }

            return false;
        }
    }
}
