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
/// <para>
/// Reading back a value the method itself has just written is valid, as the element being read is no longer uninitialized.
/// To account for that, a read is not reported when a write covering the same location is guaranteed to run before it,
/// such as a preceding <c>span.Clear()</c> or <c>span.Fill(value)</c> call, or a preceding assignment to that same element.
/// </para>
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

            // Also get the '[Conditional]' symbol, which is used to detect calls that might be removed entirely
            INamedTypeSymbol? conditionalAttributeType = context.Compilation.GetTypeByMetadataName("System.Diagnostics.ConditionalAttribute");

            WellKnownSymbols symbols = new(spanType, readOnlySpanType, conditionalAttributeType);

            // The whole body of a method is analyzed at once, rather than each operation in isolation, so that
            // each candidate read can also be inspected in the context of the code that necessarily runs before it
            context.RegisterOperationBlockAction(context =>
            {
                // Fill array parameters can only be declared by a method on a public, top-level class (see
                // 'IsWindowsRuntimeMethod'), which is a cheap way to skip most of the code being compiled
                if (context.OwningSymbol.ContainingType is not { TypeKind: TypeKind.Class, DeclaredAccessibility: Accessibility.Public, ContainingType: null })
                {
                    return;
                }

                // For a method, the parameters are known up front, so bodies that can't possibly read from a fill
                // array parameter are skipped right away. That is not the case for field and property initializers,
                // which can also reference the parameters of the primary constructor of their containing type.
                if (context.OwningSymbol is IMethodSymbol method && !HasSpanParameter(method, spanType))
                {
                    return;
                }

                foreach (IOperation operationBlock in context.OperationBlocks)
                {
                    bool allowsSuppression = AllowsSuppression(operationBlock, symbols);

                    foreach (IOperation operation in operationBlock.DescendantsAndSelf())
                    {
                        switch (operation)
                        {
                            case IPropertyReferenceOperation propertyReference:
                                AnalyzeElementRead(context, propertyReference, symbols, allowsSuppression);
                                break;
                            case IConversionOperation conversion:
                                AnalyzeSpanConversion(context, conversion, symbols, allowsSuppression);
                                break;
                            case IForEachLoopOperation forEachLoop:
                                AnalyzeForEachLoop(context, forEachLoop, symbols, allowsSuppression);
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
    /// <param name="symbols">The well known symbols used by the analyzer.</param>
    /// <param name="allowsSuppression">Whether reads preceded by a covering write can be suppressed.</param>
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
        WellKnownSymbols symbols,
        bool allowsSuppression)
    {
        // We only care about the 'Span<T>' indexer (i.e. 'span[i]'), not other properties (e.g. 'Length')
        if (!operation.Property.IsIndexer)
        {
            return;
        }

        // The indexer must be invoked directly on a write-only 'Span<T>' parameter
        if (operation.Instance is not IParameterReferenceOperation { Parameter: { } parameter } ||
            !IsFillArrayParameter(parameter, symbols))
        {
            return;
        }

        // Skip usages that only write to the element, which are valid for a fill array
        if (IsWriteOnlyElementUsage(operation))
        {
            return;
        }

        // Skip reads of an element that the method is guaranteed to have already written to
        if (allowsSuppression &&
            operation.Arguments is [{ Value: { } index }] &&
            IsPrecededByCoveringWrite(operation, ReadTarget.ForElement(parameter, symbols, index)))
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
    /// <param name="symbols">The well known symbols used by the analyzer.</param>
    /// <param name="allowsSuppression">Whether reads preceded by a covering write can be suppressed.</param>
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
        WellKnownSymbols symbols,
        bool allowsSuppression)
    {
        // The conversion must target 'ReadOnlySpan<T>' (a read-only view over the span elements)
        if (!SymbolEqualityComparer.Default.Equals(operation.Type?.OriginalDefinition, symbols.ReadOnlySpanType))
        {
            return;
        }

        // The operand must be a write-only 'Span<T>' parameter
        if (operation.Operand is not IParameterReferenceOperation { Parameter: { } parameter } ||
            !IsFillArrayParameter(parameter, symbols))
        {
            return;
        }

        // Skip conversions of a span that the method is guaranteed to have already filled entirely
        if (allowsSuppression && IsPrecededByCoveringWrite(operation, ReadTarget.ForSpan(parameter, symbols)))
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
    /// <param name="symbols">The well known symbols used by the analyzer.</param>
    /// <param name="allowsSuppression">Whether reads preceded by a covering write can be suppressed.</param>
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
        WellKnownSymbols symbols,
        bool allowsSuppression)
    {
        // The iterated collection is the span parameter, possibly wrapped in an identity conversion
        IOperation collection = operation.Collection is IConversionOperation { Operand: { } operand }
            ? operand
            : operation.Collection;

        // The iterated collection must be a write-only 'Span<T>' parameter
        if (collection is not IParameterReferenceOperation { Parameter: { } parameter } ||
            !IsFillArrayParameter(parameter, symbols))
        {
            return;
        }

        // Iterating with a writable 'ref' loop variable may be used to fill the span, so the loop
        // itself is valid. In that case, only the individual reads through the loop variable (which
        // aliases the current element) are reported. By-value and 'ref readonly' loop variables can
        // only ever read the elements, so for those the loop itself is reported instead.
        if (operation.LoopControlVariable is IVariableDeclaratorOperation { Symbol: { RefKind: RefKind.Ref } loopVariable })
        {
            AnalyzeLoopVariableReads(context, operation.Body, ReadTarget.ForAlias(parameter, symbols, loopVariable), allowsSuppression);

            return;
        }

        // Skip iterations over a span that the method is guaranteed to have already filled entirely
        if (allowsSuppression && IsPrecededByCoveringWrite(operation.Collection, ReadTarget.ForSpan(parameter, symbols)))
        {
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
    /// <param name="loopBody">The body of the <c>foreach</c> loop declaring the loop variable.</param>
    /// <param name="target">The element aliased by the writable <c>ref</c> loop variable of the loop.</param>
    /// <param name="allowsSuppression">Whether reads preceded by a covering write can be suppressed.</param>
    private static void AnalyzeLoopVariableReads(
        OperationBlockAnalysisContext context,
        IOperation loopBody,
        ReadTarget target,
        bool allowsSuppression)
    {
        foreach (IOperation operation in loopBody.DescendantsAndSelf())
        {
            // We only care about references to the loop variable, as those alias an element of the span.
            // The loop variable can't be captured by a lambda or ref reassigned, so all aliasing usages
            // of the current element are guaranteed to appear directly in the body of the loop.
            if (operation is not ILocalReferenceOperation { Local: { } local } ||
                !SymbolEqualityComparer.Default.Equals(local, target.Alias))
            {
                continue;
            }

            // Skip usages that only write to the element, which are valid for a fill array
            if (IsWriteOnlyElementUsage(operation))
            {
                continue;
            }

            // Skip reads of an element that the method is guaranteed to have already written to
            if (allowsSuppression && IsPrecededByCoveringWrite(operation, target))
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.WriteOnlySpanParameterRead,
                operation.Syntax.GetLocation(),
                target.Parameter.Name));
        }
    }

    /// <summary>
    /// Checks whether a given method has at least one by-value <see cref="System.Span{T}"/> parameter.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <param name="spanType">The <see cref="INamedTypeSymbol"/> for <see cref="System.Span{T}"/>.</param>
    /// <returns>Whether <paramref name="method"/> has at least one by-value <see cref="System.Span{T}"/> parameter.</returns>
    private static bool HasSpanParameter(IMethodSymbol method, INamedTypeSymbol spanType)
    {
        foreach (IParameterSymbol parameter in method.Parameters)
        {
            if (IsSpanParameter(parameter, spanType))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks whether a given parameter is a by-value <see cref="System.Span{T}"/> parameter.
    /// </summary>
    /// <param name="parameter">The parameter to check.</param>
    /// <param name="spanType">The <see cref="INamedTypeSymbol"/> for <see cref="System.Span{T}"/>.</param>
    /// <returns>Whether <paramref name="parameter"/> is a by-value <see cref="System.Span{T}"/> parameter.</returns>
    private static bool IsSpanParameter(IParameterSymbol parameter, INamedTypeSymbol spanType)
    {
        // Only a by-value 'System.Span<T>' is projected as a fill array (a 'ref', 'in' or 'out'
        // variant is not a valid Windows Runtime parameter, and 'ReadOnlySpan<T>' is a pass array).
        return
            parameter.RefKind is RefKind.None &&
            SymbolEqualityComparer.Default.Equals(parameter.Type.OriginalDefinition, spanType);
    }

    /// <summary>
    /// Checks whether a given parameter is a write-only <see cref="System.Span{T}"/> parameter (i.e. a parameter
    /// that is projected as a fill array in the Windows Runtime ABI).
    /// </summary>
    /// <param name="parameter">The parameter to check.</param>
    /// <param name="symbols">The well known symbols used by the analyzer.</param>
    /// <returns>Whether <paramref name="parameter"/> is a write-only fill array parameter.</returns>
    private static bool IsFillArrayParameter(IParameterSymbol parameter, WellKnownSymbols symbols)
    {
        // The parameter must belong to a method that is projected to the Windows Runtime. This also filters
        // out the parameters of nested lambdas and local functions, which are never projected (and could not
        // reference the parameters of the enclosing method anyway, as a span cannot be captured by them).
        return
            IsSpanParameter(parameter, symbols.SpanType) &&
            parameter.ContainingSymbol is IMethodSymbol method &&
            IsWindowsRuntimeMethod(method);
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

    /// <summary>
    /// Checks whether a given read from a write-only <see cref="System.Span{T}"/> parameter is preceded by a write
    /// that is guaranteed to run before it, and that covers the same location. Such a read only ever observes values
    /// that the method itself has just written, so it is valid even though the parameter is a fill array.
    /// </summary>
    /// <param name="read">The operation performing the read.</param>
    /// <param name="target">The location that <paramref name="read"/> accesses.</param>
    /// <returns>Whether <paramref name="read"/> is preceded by a write covering <paramref name="target"/>.</returns>
    /// <remarks>
    /// Only writes appearing in a statement that precedes (an ancestor of) <paramref name="read"/> in some enclosing
    /// block are considered, and only when they are unconditionally reached within that statement. This is meant to
    /// be conservative: it is fine to miss a write that does happen, but a write must never be reported as guaranteed
    /// unless it necessarily runs first, as that would silently hide an actual read of an uninitialized element.
    /// </remarks>
    private static bool IsPrecededByCoveringWrite(IOperation read, ReadTarget target)
    {
        IOperation current = read;
        IOperation? parent = read.Parent;

        while (parent is not null)
        {
            // Never look at the enclosing method when the read is inside a lambda or a local function, as those
            // can be invoked at any point. This can't normally be reached, given that a span is a 'ref struct'
            // type, and as such it cannot be captured by either of them, but it is cheap to guard against.
            if (parent is IAnonymousFunctionOperation or ILocalFunctionOperation)
            {
                return false;
            }

            // Only the statements within a block are ordered with respect to one another. Any other kind of parent
            // is just skipped, meaning the search resumes from the closest enclosing statement in the parent block.
            if (parent is IBlockOperation block)
            {
                int index = block.Operations.IndexOf(current);

                for (int i = index - 1; i >= 0; i--)
                {
                    if (IsGuaranteedCoveringWrite(block.Operations[i], target))
                    {
                        // A preceding write only covers the read if the operands it depends on can't have changed in
                        // between. If they can, no earlier write can be relied upon either, as it would span an even
                        // wider range of statements, so there is no point in continuing the search past this one.
                        return AreOperandsStable(block.Operations, i, index, target);
                    }
                }
            }

            current = parent;
            parent = parent.Parent;
        }

        return false;
    }

    /// <summary>
    /// Checks whether a given operation contains a write covering a target location that is guaranteed to be reached.
    /// </summary>
    /// <param name="operation">The operation to inspect.</param>
    /// <param name="target">The location that the write should cover.</param>
    /// <returns>Whether <paramref name="operation"/> necessarily performs a write covering <paramref name="target"/>.</returns>
    private static bool IsGuaranteedCoveringWrite(IOperation operation, ReadTarget target)
    {
        // Constructs that might not run at all can never guarantee that a write nested in them will happen
        if (IsConditionallyExecuted(operation, target.Symbols))
        {
            return false;
        }

        if (IsCoveringWrite(operation, target))
        {
            return true;
        }

        foreach (IOperation child in operation.ChildOperations)
        {
            if (IsGuaranteedCoveringWrite(child, target))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks whether a given operation is a write covering a target location.
    /// </summary>
    /// <param name="operation">The operation to inspect.</param>
    /// <param name="target">The location that the write should cover.</param>
    /// <returns>Whether <paramref name="operation"/> writes to all of <paramref name="target"/>.</returns>
    private static bool IsCoveringWrite(IOperation operation, ReadTarget target)
    {
        // 'span.Clear()' and 'span.Fill(value)' initialize every element of the span, so
        // they cover any subsequent read from it, no matter which elements it looks at.
        if (operation is IInvocationOperation { TargetMethod: { Name: "Clear", Parameters: [] } or { Name: "Fill", Parameters: [_] } } invocation &&
            SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType.OriginalDefinition, target.Symbols.SpanType) &&
            invocation.Instance is { } instance &&
            IsReferenceTo(instance, target.Parameter))
        {
            return true;
        }

        // 'span[i] = value' and 'Foo(out span[i])' initialize a single element, so they only
        // cover reads of that same element (i.e. reads through an equivalent index expression).
        if (target.Index is { } index &&
            operation is IPropertyReferenceOperation { Property.IsIndexer: true, Instance: { } spanInstance, Arguments: [{ Value: { } writeIndex }] } &&
            IsReferenceTo(spanInstance, target.Parameter) &&
            IsDefiniteWrite(operation) &&
            AreIndexesEquivalent(writeIndex, index))
        {
            return true;
        }

        // 'x = value' and 'Foo(out x)' through a writable 'ref' 'foreach' loop variable initialize
        // the element it aliases, so they cover reads of that element through the same variable.
        return
            target.Alias is { } alias &&
            IsReferenceTo(operation, alias) &&
            IsDefiniteWrite(operation);
    }

    /// <summary>
    /// Checks whether a given operation might not be reached during the execution of its parent operation.
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <param name="symbols">The well known symbols used by the analyzer.</param>
    /// <returns>Whether <paramref name="operation"/> might be skipped, or run at some later point.</returns>
    private static bool IsConditionallyExecuted(IOperation operation, WellKnownSymbols symbols)
    {
        // A call to a method annotated with '[Conditional]' is removed entirely by the compiler, arguments
        // included, when the associated preprocessor symbol is not defined for the current compilation
        return (operation is IInvocationOperation { TargetMethod: { } targetMethod } &&
                symbols.ConditionalAttributeType is { } conditionalAttributeType &&
                IsConditionalMethod(targetMethod, conditionalAttributeType)) ||
            operation is
                IConditionalOperation or            // 'if' statements and '?:' expressions
                IConditionalAccessOperation or      // '?.' and '?[]' accesses
                ICoalesceOperation or               // '??' expressions
                ICoalesceAssignmentOperation or     // '??=' expressions
                ISwitchOperation or                 // 'switch' statements
                ISwitchExpressionOperation or       // 'switch' expressions
                ILoopOperation or                   // all loops, as the body might never run
                ITryOperation or                    // 'try' blocks, as an exception might skip the rest of the body
                IAnonymousFunctionOperation or      // lambdas and anonymous methods
                ILocalFunctionOperation or          // local functions
                IBinaryOperation { OperatorKind: BinaryOperatorKind.ConditionalAnd or BinaryOperatorKind.ConditionalOr };
    }

    /// <summary>
    /// Checks whether calls to a given method are conditionally compiled.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <param name="conditionalAttributeType">The <see cref="INamedTypeSymbol"/> for <c>[Conditional]</c>.</param>
    /// <returns>Whether calls to <paramref name="method"/> might be removed by the compiler.</returns>
    private static bool IsConditionalMethod(IMethodSymbol method, INamedTypeSymbol conditionalAttributeType)
    {
        // An override cannot carry '[Conditional]' itself, but it does inherit the conditional
        // symbols of the method it overrides, so the whole chain has to be inspected here
        for (IMethodSymbol? currentMethod = method; currentMethod is not null; currentMethod = currentMethod.OverriddenMethod)
        {
            if (currentMethod.HasAttributeWithType(conditionalAttributeType))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks whether the operands a covering write depends on (i.e. the span parameter itself, and the index
    /// variable, if the target is a single element) can't have been modified between that write and a later read.
    /// </summary>
    /// <param name="statements">The statements of the block containing both the write and the read.</param>
    /// <param name="writeIndex">The index of the statement containing the write.</param>
    /// <param name="readIndex">The index of the statement containing the read.</param>
    /// <param name="target">The location that the write covers.</param>
    /// <returns>Whether the write still applies to <paramref name="target"/> when the read is reached.</returns>
    private static bool AreOperandsStable(ImmutableArray<IOperation> statements, int writeIndex, int readIndex, ReadTarget target)
    {
        ISymbol? indexSymbol = target.IndexSymbol;

        // An index that is itself a 'ref' local or parameter aliases some other storage (e.g. a field, or an
        // array element), so its value can change without any write to the symbol itself being visible here
        if (indexSymbol is ILocalSymbol { RefKind: not RefKind.None } or IParameterSymbol { RefKind: not RefKind.None })
        {
            return false;
        }

        for (int i = writeIndex; i <= readIndex; i++)
        {
            foreach (IOperation operation in statements[i].DescendantsAndSelf())
            {
                // Reassigning the span parameter would make the write apply to an entirely different buffer
                if (IsPotentialWrite(operation, target.Parameter))
                {
                    return false;
                }

                // Likewise, modifying the index variable would make the two accesses target different elements
                if (indexSymbol is not null && IsPotentialWrite(operation, indexSymbol))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Checks whether a given operation is a reference to a local or parameter that might modify its value.
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <param name="symbol">The local or parameter to look for.</param>
    /// <returns>Whether <paramref name="operation"/> might modify the value of <paramref name="symbol"/>.</returns>
    private static bool IsPotentialWrite(IOperation operation, ISymbol symbol)
    {
        return IsReferenceTo(operation, symbol) && operation.Parent switch
        {
            // 'x = value', 'x += value' and 'x ??= value'
            IAssignmentOperation assignment => ReferenceEquals(assignment.Target, operation),

            // 'x++' and 'x--'
            IIncrementOrDecrementOperation incrementOrDecrement => ReferenceEquals(incrementOrDecrement.Target, operation),

            // 'Foo(out x)' and 'Foo(ref x)'
            IArgumentOperation { Parameter.RefKind: RefKind.Out or RefKind.Ref } => true,

            // 'ref var alias = ref x', which can then be used to write to it
            IVariableInitializerOperation { Parent: IVariableDeclaratorOperation { Symbol.RefKind: RefKind.Ref } } => true,

            // '(x, y) = value' (a deconstruction assignment), where the reference is nested in the target tuple
            ITupleOperation tuple => IsDeconstructionTarget(tuple),

            _ => false
        };
    }

    /// <summary>
    /// Checks whether a given tuple is (nested in) the target of a deconstruction assignment.
    /// </summary>
    /// <param name="tuple">The tuple to check.</param>
    /// <returns>Whether <paramref name="tuple"/> is being assigned to by a deconstruction.</returns>
    private static bool IsDeconstructionTarget(ITupleOperation tuple)
    {
        IOperation current = tuple;

        // Deconstructions can be nested (e.g. '((x, y), z) = value'), so walk up all the enclosing tuples
        while (current.Parent is ITupleOperation parent)
        {
            current = parent;
        }

        return current.Parent is IDeconstructionAssignmentOperation deconstruction && ReferenceEquals(deconstruction.Target, current);
    }

    /// <summary>
    /// Checks whether a given reference to an element of a write-only <see cref="System.Span{T}"/> parameter is
    /// guaranteed to initialize that element, as opposed to just possibly writing to it.
    /// </summary>
    /// <param name="operation">The operation referencing the element (either an indexer access, or a <c>ref</c> alias).</param>
    /// <returns>Whether <paramref name="operation"/> necessarily writes to the element.</returns>
    private static bool IsDefiniteWrite(IOperation operation)
    {
        return operation.Parent switch
        {
            // 'span[i] = value' or 'x = value': the element is definitely assigned
            ISimpleAssignmentOperation simpleAssignment => ReferenceEquals(simpleAssignment.Target, operation),

            // 'Foo(out span[i])' or 'Foo(out x)': the callee has to assign the element before returning
            IArgumentOperation { Parameter.RefKind: RefKind.Out } => true,

            // Note that 'ref' arguments and writable 'ref' aliases are not definite writes: the callee (or the
            // code using the alias) might never actually write to the element, so they can't be relied upon.
            _ => false
        };
    }

    /// <summary>
    /// Checks whether a given operation is a reference to a specific local or parameter.
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <param name="symbol">The local or parameter to look for.</param>
    /// <returns>Whether <paramref name="operation"/> is a reference to <paramref name="symbol"/>.</returns>
    private static bool IsReferenceTo(IOperation operation, ISymbol symbol)
    {
        return operation switch
        {
            ILocalReferenceOperation localReference => SymbolEqualityComparer.Default.Equals(localReference.Local, symbol),
            IParameterReferenceOperation parameterReference => SymbolEqualityComparer.Default.Equals(parameterReference.Parameter, symbol),
            _ => false
        };
    }

    /// <summary>
    /// Checks whether two index expressions are guaranteed to produce the same value.
    /// </summary>
    /// <param name="left">The first index expression to compare.</param>
    /// <param name="right">The second index expression to compare.</param>
    /// <returns>Whether <paramref name="left"/> and <paramref name="right"/> necessarily refer to the same element.</returns>
    /// <remarks>
    /// Two index expressions referring to the same variable are only equivalent as long as that variable is not
    /// modified in between. It is up to callers to validate that (see <see cref="AreOperandsStable"/>).
    /// </remarks>
    private static bool AreIndexesEquivalent(IOperation left, IOperation right)
    {
        // Constant indices are equivalent when they have the same value (e.g. 'span[0]' and 'span[0]')
        if (left.ConstantValue is { HasValue: true, Value: { } leftValue } &&
            right.ConstantValue is { HasValue: true, Value: { } rightValue })
        {
            return leftValue.Equals(rightValue);
        }

        // Otherwise, only a reference to the same local or parameter is recognized (e.g. 'span[i]' in a loop)
        return (UnwrapImplicitConversions(left), UnwrapImplicitConversions(right)) switch
        {
            (ILocalReferenceOperation leftLocal, ILocalReferenceOperation rightLocal) => SymbolEqualityComparer.Default.Equals(leftLocal.Local, rightLocal.Local),
            (IParameterReferenceOperation leftParameter, IParameterReferenceOperation rightParameter) => SymbolEqualityComparer.Default.Equals(leftParameter.Parameter, rightParameter.Parameter),
            _ => false
        };
    }

    /// <summary>
    /// Unwraps all compiler inserted conversions from a given operation (e.g. the widening conversion in <c>span[byteIndex]</c>).
    /// </summary>
    /// <param name="operation">The operation to unwrap.</param>
    /// <returns>The innermost operand of <paramref name="operation"/> that is not an implicit conversion.</returns>
    private static IOperation? UnwrapImplicitConversions(IOperation? operation)
    {
        while (operation is IConversionOperation { IsImplicit: true } conversion)
        {
            operation = conversion.Operand;
        }

        return operation;
    }

    /// <summary>
    /// Checks whether reads in a given operation block can be skipped when they are preceded by a covering write.
    /// </summary>
    /// <param name="operationBlock">The operation block to inspect.</param>
    /// <param name="symbols">The well known symbols used by the analyzer.</param>
    /// <returns>Whether preceding writes can be relied upon for reads in <paramref name="operationBlock"/>.</returns>
    /// <remarks>
    /// Recognizing a preceding write relies on being able to see every write to the span parameter and to the
    /// index variables in source order. That is not possible when the body can jump around, or when it creates
    /// an indirection that could be used to modify a variable from a place the ordered scan can't account for.
    /// </remarks>
    private static bool AllowsSuppression(IOperation operationBlock, WellKnownSymbols symbols)
    {
        foreach (IOperation operation in operationBlock.DescendantsAndSelf())
        {
            switch (operation)
            {
                // A 'goto' can jump over a write that would otherwise always run before a given read
                case IBranchOperation { BranchKind: BranchKind.GoTo }:

                // A lambda or a local function can capture a variable and modify it when invoked, which
                // can happen at any point (a span itself can never be captured, as it is a 'ref struct')
                case IAnonymousFunctionOperation or ILocalFunctionOperation:

                // A pointer can be used to modify a variable without any visible write to it
                case { Type: IPointerTypeSymbol }:
                    return false;

                // A writable 'ref' alias can be used to modify whatever it points at from anywhere it is in
                // scope. An alias to an element of a span is the one exception: it can only ever be used to
                // write to that element, never to redirect the span itself or to change an index variable.
                case IVariableDeclaratorOperation { Symbol.RefKind: RefKind.Ref, Initializer.Value: { } aliasedValue }
                    when !IsSpanElementReference(aliasedValue, symbols):
                    return false;

                // The same applies to a 'ref' reassignment of an existing alias
                case ISimpleAssignmentOperation { IsRef: true, Value: { } reassignedValue }
                    when !IsSpanElementReference(reassignedValue, symbols):
                    return false;
                default:
                    break;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks whether a given operation references an element of a <see cref="System.Span{T}"/> value.
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <param name="symbols">The well known symbols used by the analyzer.</param>
    /// <returns>Whether <paramref name="operation"/> is an access to an element of a <see cref="System.Span{T}"/>.</returns>
    private static bool IsSpanElementReference(IOperation operation, WellKnownSymbols symbols)
    {
        return
            operation is IPropertyReferenceOperation { Property.IsIndexer: true, Instance.Type: { } instanceType } &&
            SymbolEqualityComparer.Default.Equals(instanceType.OriginalDefinition, symbols.SpanType);
    }

    /// <summary>
    /// The well known symbols used by the analyzer.
    /// </summary>
    /// <param name="spanType">The <see cref="INamedTypeSymbol"/> for <see cref="System.Span{T}"/>.</param>
    /// <param name="readOnlySpanType">The <see cref="INamedTypeSymbol"/> for <see cref="System.ReadOnlySpan{T}"/>.</param>
    /// <param name="conditionalAttributeType">The <see cref="INamedTypeSymbol"/> for <c>[Conditional]</c>, if available.</param>
    private readonly struct WellKnownSymbols(
        INamedTypeSymbol spanType,
        INamedTypeSymbol readOnlySpanType,
        INamedTypeSymbol? conditionalAttributeType)
    {
        /// <summary>
        /// Gets the <see cref="INamedTypeSymbol"/> for <see cref="System.Span{T}"/>.
        /// </summary>
        public INamedTypeSymbol SpanType => spanType;

        /// <summary>
        /// Gets the <see cref="INamedTypeSymbol"/> for <see cref="System.ReadOnlySpan{T}"/>.
        /// </summary>
        public INamedTypeSymbol ReadOnlySpanType => readOnlySpanType;

        /// <summary>
        /// Gets the <see cref="INamedTypeSymbol"/> for <c>[Conditional]</c>, if available.
        /// </summary>
        public INamedTypeSymbol? ConditionalAttributeType => conditionalAttributeType;
    }

    /// <summary>
    /// Describes the location that a read from a write-only <see cref="System.Span{T}"/> parameter accesses, so
    /// that writes which are guaranteed to have already initialized that same location can be recognized.
    /// </summary>
    private readonly struct ReadTarget
    {
        /// <summary>
        /// Creates a new <see cref="ReadTarget"/> instance with the specified parameters.
        /// </summary>
        /// <param name="parameter">The write-only <see cref="System.Span{T}"/> parameter being read from.</param>
        /// <param name="symbols">The well known symbols used by the analyzer.</param>
        /// <param name="index">The index of the element being read, if the read goes through the span indexer.</param>
        /// <param name="alias">The writable <c>ref</c> <c>foreach</c> loop variable aliasing the element being read, if any.</param>
        private ReadTarget(IParameterSymbol parameter, WellKnownSymbols symbols, IOperation? index, ILocalSymbol? alias)
        {
            Parameter = parameter;
            Symbols = symbols;
            Index = index;
            Alias = alias;
        }

        /// <summary>
        /// Gets the write-only <see cref="System.Span{T}"/> parameter being read from.
        /// </summary>
        public IParameterSymbol Parameter { get; }

        /// <summary>
        /// Gets the well known symbols used by the analyzer.
        /// </summary>
        public WellKnownSymbols Symbols { get; }

        /// <summary>
        /// Gets the index of the element being read, if the read goes through the span indexer.
        /// </summary>
        public IOperation? Index { get; }

        /// <summary>
        /// Gets the writable <c>ref</c> <c>foreach</c> loop variable aliasing the element being read, if any.
        /// </summary>
        public ILocalSymbol? Alias { get; }

        /// <summary>
        /// Gets the local or parameter that <see cref="Index"/> refers to, if it is a plain variable reference.
        /// </summary>
        public ISymbol? IndexSymbol => UnwrapImplicitConversions(Index) switch
        {
            ILocalReferenceOperation localReference => localReference.Local,
            IParameterReferenceOperation parameterReference => parameterReference.Parameter,
            _ => null
        };

        /// <summary>
        /// Creates a <see cref="ReadTarget"/> for a read of all the elements of a span (e.g. a <c>foreach</c> loop).
        /// </summary>
        /// <param name="parameter">The write-only <see cref="System.Span{T}"/> parameter being read from.</param>
        /// <param name="symbols">The well known symbols used by the analyzer.</param>
        /// <returns>The resulting <see cref="ReadTarget"/> value.</returns>
        public static ReadTarget ForSpan(IParameterSymbol parameter, WellKnownSymbols symbols)
        {
            return new(parameter, symbols, index: null, alias: null);
        }

        /// <summary>
        /// Creates a <see cref="ReadTarget"/> for a read of a single element through the span indexer.
        /// </summary>
        /// <param name="parameter">The write-only <see cref="System.Span{T}"/> parameter being read from.</param>
        /// <param name="symbols">The well known symbols used by the analyzer.</param>
        /// <param name="index">The index of the element being read.</param>
        /// <returns>The resulting <see cref="ReadTarget"/> value.</returns>
        public static ReadTarget ForElement(IParameterSymbol parameter, WellKnownSymbols symbols, IOperation index)
        {
            return new(parameter, symbols, index, alias: null);
        }

        /// <summary>
        /// Creates a <see cref="ReadTarget"/> for a read of the element aliased by a writable <c>ref</c> loop variable.
        /// </summary>
        /// <param name="parameter">The write-only <see cref="System.Span{T}"/> parameter being read from.</param>
        /// <param name="symbols">The well known symbols used by the analyzer.</param>
        /// <param name="alias">The writable <c>ref</c> <c>foreach</c> loop variable aliasing the element being read.</param>
        /// <returns>The resulting <see cref="ReadTarget"/> value.</returns>
        public static ReadTarget ForAlias(IParameterSymbol parameter, WellKnownSymbols symbols, ILocalSymbol alias)
        {
            return new(parameter, symbols, index: null, alias);
        }
    }
}
