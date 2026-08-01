// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace WindowsRuntime.SourceGenerator.Diagnostics;

/// <summary>
/// A diagnostic analyzer that warns when the implementation of a Windows Runtime method reads from a
/// <see cref="System.Span{T}"/> parameter, which is projected as a write-only fill array in the ABI.
/// </summary>
/// <remarks>
/// In the Windows Runtime ABI, array parameters use one of three conventions: a <see cref="System.ReadOnlySpan{T}"/> parameter
/// is a "pass array" (<c>[in]</c>, read-only), an <c>out T[]</c> parameter is a "receive array" (<c>[out]</c> with byref), and
/// a <see cref="System.Span{T}"/> parameter is a "fill array" ([out] without byref). A fill array is write-only: the
/// implementation is given a buffer allocated by the caller that it is expected to fill, and reading from it is not supported.
/// This analyzer detects the most common ways a method might read from such a parameter, while avoiding false positives on
/// write-only usages.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class WriteOnlySpanParameterAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [DiagnosticDescriptors.WriteOnlySpanParameterRead];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static context =>
        {
            // 'Span<T>' parameters are only projected as fill arrays when authoring a Windows Runtime component
            if (!context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.GetCsWinRTComponent())
            {
                return;
            }

            // Get the 'System.Span<T>' symbol (the type that is projected as a write-only fill array)
            if (context.Compilation.GetTypeByMetadataName("System.Span`1") is not { } spanType)
            {
                return;
            }

            // Get the 'System.ReadOnlySpan<T>' symbol (used to detect conversions that would allow reads)
            if (context.Compilation.GetTypeByMetadataName("System.ReadOnlySpan`1") is not { } readOnlySpanType)
            {
                return;
            }

            // The whole body of a method is analyzed at once, rather than each operation in isolation, so that
            // each candidate read can also be inspected in the context of the code that necessarily runs before it
            context.RegisterOperationBlockAction(context =>
            {
                // Only look at methods that are part of the Windows Runtime ABI surface and that actually have
                // at least one fill array parameter, which is the only thing this analyzer is concerned with
                if (context.OwningSymbol is not IMethodSymbol method ||
                    !HasFillArrayParameter(method, spanType) ||
                    !IsWindowsRuntimeMethod(method))
                {
                    return;
                }

                foreach (IOperation operationBlock in context.OperationBlocks)
                {
                    foreach (IOperation operation in operationBlock.DescendantsAndSelf())
                    {
                        switch (operation)
                        {
                            case IPropertyReferenceOperation propertyReference:
                                AnalyzeElementRead(context, propertyReference, method, spanType);
                                break;
                            case IConversionOperation conversion:
                                AnalyzeSpanConversion(context, conversion, method, spanType, readOnlySpanType);
                                break;
                            case IForEachLoopOperation forEachLoop:
                                AnalyzeForEachLoop(context, forEachLoop, method, spanType);
                                break;
                            default:
                                break;
                        }
                    }
                }
            });
        });
    }

    /// <summary>
    /// Analyzes a property reference, to detect reads of an element of a write-only <see cref="System.Span{T}"/> parameter.
    /// </summary>
    /// <param name="context">The context to report diagnostics to.</param>
    /// <param name="operation">The property reference to analyze.</param>
    /// <param name="method">The method being analyzed.</param>
    /// <param name="spanType">The <see cref="INamedTypeSymbol"/> for <see cref="System.Span{T}"/>.</param>
    /// <remarks>
    /// This handles reading the value (or a readonly reference) of an element, such as:
    /// <code>
    /// _ = span[i];
    /// Foo(span[i]);
    /// ref readonly var x = ref span[i];
    /// Foo(in span[i]);
    /// span[i]++;
    /// span[i] += 1;
    /// </code>
    /// </remarks>
    private static void AnalyzeElementRead(
        OperationBlockAnalysisContext context,
        IPropertyReferenceOperation operation,
        IMethodSymbol method,
        INamedTypeSymbol spanType)
    {
        // We only care about the 'Span<T>' indexer (i.e. 'span[i]'), not other properties (e.g. 'Length')
        if (!operation.Property.IsIndexer)
        {
            return;
        }

        // The indexer must be invoked directly on a write-only 'Span<T>' parameter
        if (operation.Instance is not IParameterReferenceOperation { Parameter: { } parameter } ||
            !IsFillArrayParameter(parameter, method, spanType))
        {
            return;
        }

        // Skip usages that only write to the element, which are valid for a fill array
        if (IsWriteOnlyElementUsage(operation))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.WriteOnlySpanParameterRead,
            operation.Syntax.GetLocation(),
            parameter.Name));
    }

    /// <summary>
    /// Analyzes a conversion, to detect a write-only <see cref="System.Span{T}"/> parameter being converted to
    /// <see cref="System.ReadOnlySpan{T}"/>, which would allow reading all of its elements.
    /// </summary>
    /// <param name="context">The context to report diagnostics to.</param>
    /// <param name="operation">The conversion to analyze.</param>
    /// <param name="method">The method being analyzed.</param>
    /// <param name="spanType">The <see cref="INamedTypeSymbol"/> for <see cref="System.Span{T}"/>.</param>
    /// <param name="readOnlySpanType">The <see cref="INamedTypeSymbol"/> for <see cref="System.ReadOnlySpan{T}"/>.</param>
    /// <remarks>
    /// This handles converting the span to <see cref="System.ReadOnlySpan{T}"/>, such as:
    /// <code>
    /// ReadOnlySpan&lt;int&gt; readOnlySpan = span;
    /// Foo((ReadOnlySpan&lt;int&gt;)span);
    /// </code>
    /// </remarks>
    private static void AnalyzeSpanConversion(
        OperationBlockAnalysisContext context,
        IConversionOperation operation,
        IMethodSymbol method,
        INamedTypeSymbol spanType,
        INamedTypeSymbol readOnlySpanType)
    {
        // The conversion must target 'ReadOnlySpan<T>' (a read-only view over the span elements)
        if (!SymbolEqualityComparer.Default.Equals(operation.Type?.OriginalDefinition, readOnlySpanType))
        {
            return;
        }

        // The operand must be a write-only 'Span<T>' parameter
        if (operation.Operand is not IParameterReferenceOperation { Parameter: { } parameter } ||
            !IsFillArrayParameter(parameter, method, spanType))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.WriteOnlySpanParameterRead,
            operation.Syntax.GetLocation(),
            parameter.Name));
    }

    /// <summary>
    /// Analyzes a <c>foreach</c> loop, to detect iteration over a write-only <see cref="System.Span{T}"/> parameter.
    /// </summary>
    /// <param name="context">The context to report diagnostics to.</param>
    /// <param name="operation">The <c>foreach</c> loop to analyze.</param>
    /// <param name="method">The method being analyzed.</param>
    /// <param name="spanType">The <see cref="INamedTypeSymbol"/> for <see cref="System.Span{T}"/>.</param>
    /// <remarks>
    /// This handles iterating over the span, which reads each element, such as:
    /// <code>
    /// foreach (int x in span)
    /// {
    /// }
    /// </code>
    /// It also handles reading the current element through a writable <c>ref</c> loop variable, such as:
    /// <code>
    /// foreach (ref int x in span)
    /// {
    ///     int y = x;
    /// }
    /// </code>
    /// </remarks>
    private static void AnalyzeForEachLoop(
        OperationBlockAnalysisContext context,
        IForEachLoopOperation operation,
        IMethodSymbol method,
        INamedTypeSymbol spanType)
    {
        // The iterated collection is the span parameter, possibly wrapped in an identity conversion
        IOperation collection = operation.Collection is IConversionOperation { Operand: { } operand }
            ? operand
            : operation.Collection;

        // The iterated collection must be a write-only 'Span<T>' parameter
        if (collection is not IParameterReferenceOperation { Parameter: { } parameter } ||
            !IsFillArrayParameter(parameter, method, spanType))
        {
            return;
        }

        // Iterating with a writable 'ref' loop variable may be used to fill the span, so the loop
        // itself is valid. In that case, only the individual reads through the loop variable (which
        // aliases the current element) are reported. By-value and 'ref readonly' loop variables can
        // only ever read the elements, so for those the loop itself is reported instead.
        if (operation.LoopControlVariable is IVariableDeclaratorOperation { Symbol: { RefKind: RefKind.Ref } loopVariable })
        {
            AnalyzeLoopVariableReads(context, operation.Body, parameter, loopVariable);

            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.WriteOnlySpanParameterRead,
            operation.Collection.Syntax.GetLocation(),
            parameter.Name));
    }

    /// <summary>
    /// Analyzes the body of a <c>foreach</c> loop iterating over a write-only <see cref="System.Span{T}"/> parameter
    /// with a writable <c>ref</c> loop variable, to detect reads of the current element through that variable.
    /// </summary>
    /// <param name="context">The context to report diagnostics to.</param>
    /// <param name="loopBody">The body of the <c>foreach</c> loop declaring <paramref name="loopVariable"/>.</param>
    /// <param name="parameter">The write-only <see cref="System.Span{T}"/> parameter being iterated over.</param>
    /// <param name="loopVariable">The writable <c>ref</c> loop variable, aliasing the current element.</param>
    private static void AnalyzeLoopVariableReads(
        OperationBlockAnalysisContext context,
        IOperation loopBody,
        IParameterSymbol parameter,
        ILocalSymbol loopVariable)
    {
        foreach (IOperation operation in loopBody.DescendantsAndSelf())
        {
            // We only care about references to the loop variable, as those alias an element of the span.
            // The loop variable can't be captured by a lambda or ref reassigned, so all aliasing usages
            // of the current element are guaranteed to appear directly in the body of the loop.
            if (operation is not ILocalReferenceOperation { Local: { } local } ||
                !SymbolEqualityComparer.Default.Equals(local, loopVariable))
            {
                continue;
            }

            // Skip usages that only write to the element, which are valid for a fill array
            if (IsWriteOnlyElementUsage(operation))
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.WriteOnlySpanParameterRead,
                operation.Syntax.GetLocation(),
                parameter.Name));
        }
    }

    /// <summary>
    /// Checks whether a given method has at least one write-only <see cref="System.Span{T}"/> parameter.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <param name="spanType">The <see cref="INamedTypeSymbol"/> for <see cref="System.Span{T}"/>.</param>
    /// <returns>Whether <paramref name="method"/> has at least one write-only fill array parameter.</returns>
    private static bool HasFillArrayParameter(IMethodSymbol method, INamedTypeSymbol spanType)
    {
        foreach (IParameterSymbol parameter in method.Parameters)
        {
            if (IsFillArrayParameter(parameter, method, spanType))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks whether a given parameter is a write-only <see cref="System.Span{T}"/> parameter of a given method
    /// (i.e. a parameter that is projected as a fill array in the Windows Runtime ABI).
    /// </summary>
    /// <param name="parameter">The parameter to check.</param>
    /// <param name="method">The method being analyzed.</param>
    /// <param name="spanType">The <see cref="INamedTypeSymbol"/> for <see cref="System.Span{T}"/>.</param>
    /// <returns>Whether <paramref name="parameter"/> is a write-only fill array parameter of <paramref name="method"/>.</returns>
    private static bool IsFillArrayParameter(IParameterSymbol parameter, IMethodSymbol method, INamedTypeSymbol spanType)
    {
        // The parameter must belong to the method being analyzed, and not to a nested lambda or local function,
        // which are never projected (and could not reference the parameters of the method anyway, as a span
        // is a 'ref struct' type, and as such it cannot be captured by any of them).
        if (!SymbolEqualityComparer.Default.Equals(parameter.ContainingSymbol, method))
        {
            return false;
        }

        // The parameter must be a by-value 'System.Span<T>': only this is projected as a fill array (a 'ref',
        // 'in' or 'out' variant is not a valid Windows Runtime parameter, and 'ReadOnlySpan<T>' is a pass array).
        return
            parameter.RefKind is RefKind.None &&
            SymbolEqualityComparer.Default.Equals(parameter.Type.OriginalDefinition, spanType);
    }

    /// <summary>
    /// Checks whether a given method is part of the Windows Runtime ABI surface of an authored component.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <returns>Whether <paramref name="method"/> is projected to the Windows Runtime.</returns>
    private static bool IsWindowsRuntimeMethod(IMethodSymbol method)
    {
        // The method must be an ordinary method, a constructor, or an explicit interface
        // implementation (and not e.g. a local function, a lambda, an operator or a property accessor).
        if (method.MethodKind is not (MethodKind.Ordinary or MethodKind.Constructor or MethodKind.ExplicitInterfaceImplementation))
        {
            return false;
        }

        // The containing type must be a public, top-level class (i.e. an authored runtime class). Other type
        // kinds can't have method bodies with fill array parameters, and nested types are never projected.
        if (method.ContainingType is not { TypeKind: TypeKind.Class, DeclaredAccessibility: Accessibility.Public, ContainingType: null })
        {
            return false;
        }

        // Public methods (including overrides and implicit interface implementations) are part of the ABI surface
        if (method.DeclaredAccessibility is Accessibility.Public)
        {
            return true;
        }

        // Explicit interface implementations are also part of the ABI surface, through the interfaces they
        // implement. Only public interfaces are considered, as those are the ones projected to the Windows Runtime.
        foreach (IMethodSymbol implementedMethod in method.ExplicitInterfaceImplementations)
        {
            if (implementedMethod.ContainingType.DeclaredAccessibility is Accessibility.Public)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks whether a given reference to an element of a write-only <see cref="System.Span{T}"/> parameter is
    /// only used to write to that element (which is valid for a fill array), as opposed to also reading its value.
    /// </summary>
    /// <param name="operation">The operation referencing the element (either an indexer access, or a <c>ref</c> alias).</param>
    /// <returns>Whether <paramref name="operation"/> is a write-only usage of the element.</returns>
    private static bool IsWriteOnlyElementUsage(IOperation operation)
    {
        // The examples below use an indexer access for the element reference, but the same reasoning
        // applies verbatim to a 'ref' alias to the element (e.g. a writable 'foreach' loop variable).
        return operation.Parent switch
        {
            // 'span[i] = value' (the element is the target of the assignment): this only writes to the element.
            // Note that a compound assignment ('span[i] += value') is not handled here, as it also reads.
            ISimpleAssignmentOperation simpleAssignment => ReferenceEquals(simpleAssignment.Target, operation),

            // 'Foo(out span[i])' or 'Foo(ref span[i])': the callee may write to the element, and we can't tell
            // whether it also reads it, so we conservatively treat these as write-only to avoid false positives.
            IArgumentOperation { Parameter.RefKind: RefKind.Out or RefKind.Ref } => true,

            // 'ref var x = ref span[i]' (a writable 'ref' alias to the element): the alias may be used to write
            // to the element, so we also conservatively treat it as write-only (a 'ref readonly' alias cannot).
            IVariableInitializerOperation { Parent: IVariableDeclaratorOperation { Symbol.RefKind: RefKind.Ref } } => true,

            // Any other usage reads the value (e.g. as a value, a 'ref readonly' alias, an 'in' argument, a
            // compound assignment, or an increment/decrement), which is not valid for a write-only fill array.
            _ => false
        };
    }
}
