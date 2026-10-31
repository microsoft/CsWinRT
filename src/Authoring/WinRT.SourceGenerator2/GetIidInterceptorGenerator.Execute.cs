// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using WindowsRuntime.SourceGenerator.Models;

#pragma warning disable RSEXPERIMENTAL002, IDE0046

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
        public static bool IsTargetNodeValid(SyntaxNode node, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            // Check the shape of the call first, as that's the cheapest test
            if (node is not InvocationExpressionSyntax { ArgumentList.Arguments.Count: 0 } invocation ||
                !(invocation.Expression is SimpleNameSyntax { Identifier.ValueText: "GetIid" } ||
                  invocation.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "GetIid" }))
            {
                return false;
            }

            // Interceptors can only be declared in namespaces enabled via 'InterceptorsNamespaces', or the compiler reports an error.
            // The targets only enable ours when the project hasn't opted out. The analyzer config options can't be read from here, so
            // checking this lets us skip all the semantic analysis below (which is the expensive part) for projects that opted out.
            return node.SyntaxTree.Options.IsInterceptorsNamespaceEnabled(GeneratedNamespace);
        }

        /// <summary>
        /// Analyzes a candidate call and releases all compiler objects before incremental caching.
        /// </summary>
        /// <param name="context">The syntax context.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>Its precomputed IIDs and compiler-provided location, when interceptable.</returns>
        public static GetIidCallInfo? GetCallInfo(
            GeneratorSyntaxContext context,
            CancellationToken token)
        {
            InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)context.Node;
            SemanticModel model = context.SemanticModel;
            Compilation compilation = model.Compilation;

            // Only intercept the real 'WindowsRuntimeMarshal.GetIid<T>()' method from 'WinRT.Runtime' (not any same-named method
            // or a type of the same name from another assembly), and skip calls in expression trees, which can't be intercepted.
            if (model.GetSymbolInfo(invocation, token).Symbol is not IMethodSymbol
                {
                    IsStatic: true,
                    Arity: 1,
                    Parameters.Length: 0,
                    ReturnsByRef: false,
                    ReturnsByRefReadonly: false
                } method ||
                method.ContainingAssembly.Identity.Name != "WinRT.Runtime" ||
                !SymbolEqualityComparer.Default.Equals(method.ContainingType, compilation.GetTypeByMetadataName("WindowsRuntime.InteropServices.WindowsRuntimeMarshal")) ||
                !SymbolEqualityComparer.Default.Equals(method.ReturnType, compilation.GetTypeByMetadataName("System.Guid")) ||
                IsInExpressionTree(model.GetOperation(invocation, token), compilation))
            {
                return null;
            }

            WindowsRuntimeIidResolver resolver = new(compilation, method.ContainingAssembly, token);

            // The XAML mode comes from the analyzer options, which a syntax transform can't read. Computing both IIDs here (they
            // share the resolver's cached metadata) lets the pipeline pick the right one later, in a cheap step that doesn't rerun
            // this analysis when the mode changes. It's also needed to tell whether the IID depends on the mode at all (see below).
            Guid? winUIIid = resolver.TryGetIid(method.TypeArguments[0], false, out Guid winUIValue) ? winUIValue : null;
            Guid? uwpIid = resolver.TryGetIid(method.TypeArguments[0], true, out Guid uwpValue) ? uwpValue : null;

            // A reusable library cannot bake in a mapping selected by its eventual publishing application's XAML mode
            if ((winUIIid is null && uwpIid is null) ||
                (compilation.Options.OutputKind == OutputKind.DynamicallyLinkedLibrary && winUIIid != uwpIid))
            {
                return null;
            }

            // Roslyn can't always provide a location to intercept (e.g. if the call isn't in a source file with a usable path,
            // or if its syntax can't be targeted), in which case the call can't be intercepted, so we bail and leave it alone.
            if (model.GetInterceptableLocation(invocation, token) is not InterceptableLocation location)
            {
                return null;
            }

            return new(winUIIid, uwpIid, location.Version, location.Data, method.TypeArguments[0].GetCrefTypeName());
        }

        /// <summary>
        /// Gets whether the project allows generating <c>GetIid&lt;T&gt;()</c> interceptors.
        /// </summary>
        /// <param name="options">The analyzer configuration.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>Whether interceptors should be generated.</returns>
        public static bool GetGenerateInterceptors(AnalyzerConfigOptionsProvider options, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            return options.GlobalOptions.GetCsWinRTGenerateGetIidInterceptors();
        }

        /// <summary>
        /// Gets the project's XAML projection mode.
        /// </summary>
        /// <param name="options">The analyzer configuration.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>Whether the project uses UWP XAML mappings.</returns>
        public static bool GetUseWindowsUIXamlProjections(AnalyzerConfigOptionsProvider options, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            return options.GlobalOptions.GetCsWinRTUseWindowsUIXamlProjections();
        }

        /// <summary>
        /// Selects the interceptor IID for the project's XAML mode.
        /// </summary>
        /// <param name="data">The analyzed call and selected XAML mode.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>The interceptor input, if the selected IID is available.</returns>
        public static GetIidInterceptorInfo? GetInterceptorInfo(
            (GetIidCallInfo Call, bool UseWindowsUIXamlProjections) data,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            GetIidCallInfo call = data.Call;
            Guid? iid = data.UseWindowsUIXamlProjections ? call.UwpIid : call.WinUIIid;

            return iid is Guid value ? new GetIidInterceptorInfo(value, call.Version, call.Data, call.TypeName) : null;
        }

        /// <summary>
        /// Wraps collected interceptor inputs with value equality.
        /// </summary>
        /// <param name="items">The collected inputs.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>The value-equatable inputs.</returns>
        public static EquatableArray<GetIidInterceptorInfo> GetEquatableInterceptorInfo(
            ImmutableArray<GetIidInterceptorInfo> items,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            return items.AsEquatableArray();
        }

        /// <summary>
        /// Checks both explicit lambdas and compiler-translated query lambdas for expression-tree conversion.
        /// </summary>
        /// <param name="operation">The invocation operation.</param>
        /// <param name="compilation">The consuming compilation.</param>
        /// <returns>Whether this call occurs within an expression tree.</returns>
        private static bool IsInExpressionTree(IOperation? operation, Compilation compilation)
        {
            INamedTypeSymbol? expressionType = compilation.GetTypeByMetadataName("System.Linq.Expressions.Expression`1");

            for (; operation is not null; operation = operation.Parent)
            {
                if (operation is IConversionOperation { Type: INamedTypeSymbol type } &&
                    SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, expressionType))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
