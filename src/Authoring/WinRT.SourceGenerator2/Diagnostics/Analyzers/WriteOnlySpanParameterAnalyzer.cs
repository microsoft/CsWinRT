// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

            WellKnownSymbols symbols = new(
                spanType,
                readOnlySpanType,
                conditionalAttributeType,
                context.Compilation.GetTypeByMetadataName("WindowsRuntime.InteropServices.WindowsRuntimeReferenceAssemblyAttribute"),
                context.Compilation.GetTypeByMetadataName("WindowsRuntime.WindowsRuntimeTypeAttribute"));

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
                    bool allowsSuppression = AllowsSuppression(operationBlock);

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
            !IsFillArrayParameter(parameter, symbols) ||
            IsUnevaluated(operation, symbols))
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
            IsPrecededByCoveringWrite(operation, ReadTarget.ForElement(parameter, symbols, index).WithAccess(operation)))
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
            !IsFillArrayParameter(parameter, symbols) ||
            IsUnevaluated(operation, symbols))
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
            !IsFillArrayParameter(parameter, symbols) ||
            IsUnevaluated(operation, symbols))
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
                !SymbolEqualityComparer.Default.Equals(local, target.Alias) ||
                IsUnevaluated(operation, target.Symbols))
            {
                continue;
            }

            // Skip usages that only write to the element, which are valid for a fill array
            if (IsWriteOnlyElementUsage(operation))
            {
                continue;
            }

            // Skip reads of an element that the method is guaranteed to have already written to
            if (allowsSuppression && IsPrecededByCoveringWrite(operation, target.WithAccess(operation)))
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
            IsWindowsRuntimeMethod(method, symbols);
    }

    /// <summary>
    /// Checks whether a given method is part of the Windows Runtime ABI surface of an authored component.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <param name="symbols">The well known symbols used by the analyzer.</param>
    /// <returns>Whether <paramref name="method"/> is projected to the Windows Runtime.</returns>
    private static bool IsWindowsRuntimeMethod(IMethodSymbol method, WellKnownSymbols symbols)
    {
        // The method must be an ordinary method, a constructor, or an explicit interface
        // implementation (and not e.g. a local function, a lambda, an operator or a property accessor).
        if (method.MethodKind is not (MethodKind.Ordinary or MethodKind.Constructor or MethodKind.ExplicitInterfaceImplementation))
        {
            return false;
        }

        // The containing type must be a public, top-level class (i.e. an authored runtime class). Other type
        // kinds can't have method bodies with fill array parameters, and nested types are never projected.
        if (method.ContainingType is not { TypeKind: TypeKind.Class, DeclaredAccessibility: Accessibility.Public, ContainingType: null } ||
            IsAbiNamespace(method.ContainingNamespace))
        {
            return false;
        }

        // Public methods (including overrides and implicit interface implementations) are part of the ABI surface
        if (method.DeclaredAccessibility is Accessibility.Public)
        {
            return true;
        }

        // Explicit implementations are exported through authored or projected Windows Runtime interfaces,
        // not through arbitrary public .NET contracts (such as 'ISpanFormattable').
        foreach (IMethodSymbol implementedMethod in method.ExplicitInterfaceImplementations)
        {
            INamedTypeSymbol interfaceType = implementedMethod.ContainingType;

            if (interfaceType is not { DeclaredAccessibility: Accessibility.Public, ContainingType: null } ||
                IsAbiNamespace(interfaceType.ContainingNamespace))
            {
                continue;
            }

            if (SymbolEqualityComparer.Default.Equals(interfaceType.ContainingAssembly, method.ContainingAssembly) ||
                (symbols.ReferenceAssemblyAttributeType is { } referenceAssemblyAttributeType &&
                 interfaceType.ContainingAssembly.HasAttributeWithType(referenceAssemblyAttributeType)) ||
                (symbols.WindowsRuntimeTypeAttributeType is { } windowsRuntimeTypeAttributeType &&
                 interfaceType.HasAttributeWithType(windowsRuntimeTypeAttributeType)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks whether a namespace belongs to the implementation-only ABI namespace tree.
    /// </summary>
    /// <param name="namespaceSymbol">The namespace to check.</param>
    /// <returns>Whether the WinMD generator excludes this namespace.</returns>
    private static bool IsAbiNamespace(INamespaceSymbol namespaceSymbol)
    {
        while (!namespaceSymbol.IsGlobalNamespace)
        {
            if (namespaceSymbol.ContainingNamespace.IsGlobalNamespace)
            {
                return namespaceSymbol.Name == "ABI";
            }

            namespaceSymbol = namespaceSymbol.ContainingNamespace;
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
        // Accessing a field of a value type only computes its address. A reference-type receiver, on the
        // other hand, must be read even when the field reached through that reference is only assigned.
        if (operation.Parent is IFieldReferenceOperation field &&
            ReferenceEquals(field.Instance, operation) &&
            operation.Type is { IsValueType: true })
        {
            return IsWriteOnlyElementUsage(field);
        }

        // An inline-array element likewise denotes storage within the containing value. A range access
        // produces a span instead, so leave that view-producing case under the existing conservative rule.
        if (operation.Parent is IInlineArrayAccessOperation inlineArrayAccess &&
            ReferenceEquals(inlineArrayAccess.Instance, operation) &&
            inlineArrayAccess.Argument.Type is not { Name: "Range", ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true } })
        {
            return IsWriteOnlyElementUsage(inlineArrayAccess);
        }

        if (operation.Parent is IConditionalOperation { IsRef: true } conditional &&
            !ReferenceEquals(conditional.Condition, operation))
        {
            return IsWriteOnlyElementUsage(conditional);
        }

        // The examples below use an indexer access for the element reference, but the same reasoning
        // applies verbatim to a 'ref' alias to the element (e.g. a writable 'foreach' loop variable).
        return operation.Parent switch
        {
            // Rebinding a writable alias does not read its new referent, unless the result of the
            // ref assignment is itself consumed by value (e.g. '_ = (slot = ref span[i])').
            ISimpleAssignmentOperation { IsRef: true } assignment =>
                ReferenceEquals(assignment.Target, operation) ||
                (assignment.Target is ILocalReferenceOperation { Local.RefKind: RefKind.Ref } or
                                      IParameterReferenceOperation { Parameter.RefKind: RefKind.Ref } or
                                      IFieldReferenceOperation { Field.RefKind: RefKind.Ref } &&
                 (assignment.Parent is IExpressionStatementOperation || IsWriteOnlyElementUsage(assignment))),

            // 'span[i] = value' (the element is the target of the assignment): this only writes to the element.
            // Note that a compound assignment ('span[i] += value') is not handled here, as it also reads.
            ISimpleAssignmentOperation simpleAssignment => ReferenceEquals(simpleAssignment.Target, operation),

            // 'Foo(out span[i])' or 'Foo(ref span[i])': the callee may write to the element, and we can't tell
            // whether it also reads it, so we conservatively treat these as write-only to avoid false positives.
            IArgumentOperation { Parameter.RefKind: RefKind.Out or RefKind.Ref } => true,

            // 'ref var x = ref span[i]' (a writable 'ref' alias to the element): the alias may be used to write
            // to the element, so we also conservatively treat it as write-only (a 'ref readonly' alias cannot).
            IVariableInitializerOperation { Parent: IVariableDeclaratorOperation { Symbol.RefKind: RefKind.Ref } } => true,

            // Deconstruction targets do not read the old element value.
            ITupleOperation tuple => IsDeconstructionTarget(tuple),

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
                ReadTarget spanTarget = ReadTarget.ForSpan(target.Parameter, target.Symbols);

                for (int i = index - 1; i >= 0; i--)
                {
                    // A whole-span write does not depend on the index used by this particular read.
                    if (target.Index is not null && IsGuaranteedCoveringWrite(block.Operations[i], spanTarget))
                    {
                        return AreOperandsStable(block.Operations, i, index, spanTarget);
                    }

                    if (IsGuaranteedCoveringWrite(block.Operations[i], target))
                    {
                        if (AreOperandsStable(block.Operations, i, index, target))
                        {
                            return true;
                        }

                        if (target.Index is null)
                        {
                            return false;
                        }

                        // An unstable index invalidates earlier element writes, but an earlier whole-span write
                        // may still cover this read. Continue looking for one, checking the span's own stability.
                        target = spanTarget;
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
            return IsOriginalLoopBuffer(operation, target);
        }

        // Element and field assignments only cover the same element (through an equivalent index),
        // and a field write must cover the field path being read, not just some part of the element.
        if (target.Index is { } index &&
            target.Access is { } elementAccess &&
            operation is IPropertyReferenceOperation { Property.IsIndexer: true, Instance: { } spanInstance, Arguments: [{ Value: { } writeIndex }] } &&
            IsReferenceTo(spanInstance, target.Parameter) &&
            IsDefiniteFieldWrite(operation, elementAccess) &&
            AreIndexesEquivalent(writeIndex, index))
        {
            return true;
        }

        // 'x = value' and 'Foo(out x)' through a writable 'ref' 'foreach' loop variable initialize
        // the element it aliases, so they cover reads of that element through the same variable.
        return
            target.Alias is { } alias &&
            target.Access is { } aliasAccess &&
            IsReferenceTo(operation, alias) &&
            IsDefiniteFieldWrite(operation, aliasAccess);
    }

    /// <summary>
    /// Checks whether an element or value-type field write covers the field path accessed by a read.
    /// </summary>
    /// <param name="write">The indexer or loop-variable reference at the root of the potential write.</param>
    /// <param name="read">The indexer or loop-variable reference at the root of the read.</param>
    /// <returns>Whether the write initializes the entire value being read.</returns>
    private static bool IsDefiniteFieldWrite(IOperation write, IOperation read)
    {
        // The written path must be a prefix of the read path. Walking both operation chains avoids
        // allocating paths and stops at reference-type receivers, whose fields are not part of the buffer.
        while (write.Parent is IFieldReferenceOperation writtenField &&
               ReferenceEquals(writtenField.Instance, write) &&
               write.Type is { IsValueType: true })
        {
            if (read.Parent is not IFieldReferenceOperation readField ||
                !ReferenceEquals(readField.Instance, read) ||
                read.Type is not { IsValueType: true } ||
                !SymbolEqualityComparer.Default.Equals(
                    writtenField.Field.CorrespondingTupleField ?? writtenField.Field,
                    readField.Field.CorrespondingTupleField ?? readField.Field))
            {
                return false;
            }

            write = writtenField;
            read = readField;
        }

        return IsDefiniteWrite(write);
    }

    /// <summary>
    /// Checks whether a whole-span write can initialize the buffer captured by a ref foreach loop.
    /// </summary>
    /// <param name="write">The whole-span write to check.</param>
    /// <param name="target">The location that the write should cover.</param>
    /// <returns>Whether no loop iteration can redirect the span before its original buffer is initialized.</returns>
    private static bool IsOriginalLoopBuffer(IOperation write, ReadTarget target)
    {
        if (target.Alias is null)
        {
            return true;
        }

        for (IOperation? current = write.Parent; current is not null; current = current.Parent)
        {
            if (current is not IForEachLoopOperation { LoopControlVariable: IVariableDeclaratorOperation { Symbol: { } loopVariable } } loop ||
                !SymbolEqualityComparer.Default.Equals(loopVariable, target.Alias))
            {
                continue;
            }

            foreach (IOperation operation in loop.Body.DescendantsAndSelf())
            {
                // A later iteration can observe a reassignment anywhere in the body. It is safe only when
                // this write necessarily precedes that reassignment, initializing the original buffer first.
                if (IsPotentialWrite(operation, target.Parameter) &&
                    !IsUnevaluated(operation, target.Symbols) &&
                    !IsGuaranteedBefore(write, operation, target.Symbols))
                {
                    return false;
                }
            }

            break;
        }

        return true;
    }

    /// <summary>
    /// Checks whether a write unconditionally precedes an operation in a common block.
    /// </summary>
    /// <param name="write">The write that must run first.</param>
    /// <param name="operation">The operation that must be preceded by the write.</param>
    /// <param name="symbols">The well known symbols used by the analyzer.</param>
    /// <returns>Whether reaching <paramref name="operation"/> requires executing <paramref name="write"/> first.</returns>
    private static bool IsGuaranteedBefore(IOperation write, IOperation operation, WellKnownSymbols symbols)
    {
        for (IOperation? current = write; current?.Parent is { } parent; current = parent)
        {
            if (IsConditionallyExecuted(current, symbols))
            {
                return false;
            }

            if (parent is not IBlockOperation block)
            {
                continue;
            }

            IOperation statement = operation;

            while (statement.Parent is { } statementParent && !ReferenceEquals(statementParent, block))
            {
                statement = statementParent;
            }

            int writeIndex = block.Operations.IndexOf(current);

            if (writeIndex >= 0 && block.Operations.IndexOf(statement) > writeIndex)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks whether a given operation might not be reached during the execution of its parent operation.
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <param name="symbols">The well known symbols used by the analyzer.</param>
    /// <returns>Whether <paramref name="operation"/> might be skipped, or run at some later point.</returns>
    private static bool IsConditionallyExecuted(IOperation operation, WellKnownSymbols symbols)
    {
        return (operation is IInvocationOperation invocation && IsOmittedCall(invocation, symbols)) ||
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
                IBinaryOperation { OperatorKind: BinaryOperatorKind.ConditionalAnd or BinaryOperatorKind.ConditionalOr } or

                // Interpolated strings using a handler that can report failure, as the holes are not evaluated then
                IInterpolatedStringHandlerCreationOperation { HandlerCreationHasSuccessParameter: true } or
                IInterpolatedStringHandlerCreationOperation { HandlerAppendCallsReturnBool: true };
    }

    /// <summary>
    /// Checks whether an operation is in an expression that the compiler does not evaluate.
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <param name="symbols">The well known symbols used by the analyzer.</param>
    /// <returns>Whether <paramref name="operation"/> is not evaluated.</returns>
    private static bool IsUnevaluated(IOperation operation, WellKnownSymbols symbols)
    {
        for (IOperation? current = operation; current is not null; current = current.Parent)
        {
            if (current is INameOfOperation ||
                (current is IInvocationOperation invocation && IsOmittedCall(invocation, symbols)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks whether a call is removed entirely by the compiler, arguments included.
    /// </summary>
    /// <param name="invocation">The invocation to check.</param>
    /// <param name="symbols">The well known symbols used by the analyzer.</param>
    /// <returns>Whether <paramref name="invocation"/> does not survive compilation.</returns>
    private static bool IsOmittedCall(IInvocationOperation invocation, WellKnownSymbols symbols)
    {
        IMethodSymbol method = invocation.TargetMethod;

        // A 'partial void' method that has no implementing declaration has all of its call sites removed
        if (method is { IsPartialDefinition: true, PartialImplementationPart: null })
        {
            return true;
        }

        if (symbols.ConditionalAttributeType is not { } conditionalAttributeType)
        {
            return false;
        }

        bool hasConditionalAttribute = false;

        // Any defined conditional symbol preserves the call, including symbols inherited from an override.
        for (IMethodSymbol? currentMethod = method; currentMethod is not null; currentMethod = currentMethod.OverriddenMethod)
        {
            foreach (AttributeData attribute in currentMethod.GetAttributes())
            {
                if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, conditionalAttributeType))
                {
                    continue;
                }

                hasConditionalAttribute = true;

                if (attribute.ConstructorArguments is [{ Value: string symbolName }] &&
                    IsConditionalSymbolDefined(invocation.Syntax.SyntaxTree, symbolName))
                {
                    return false;
                }
            }
        }

        return hasConditionalAttribute;
    }

    /// <summary>
    /// Resolves a conditional symbol in the call-site file, including active local directives.
    /// </summary>
    /// <param name="tree">The syntax tree containing the call.</param>
    /// <param name="symbolName">The conditional symbol to resolve.</param>
    /// <returns>Whether the symbol is defined in <paramref name="tree"/>.</returns>
    private static bool IsConditionalSymbolDefined(SyntaxTree tree, string symbolName)
    {
        bool isDefined = false;

        foreach (string definedSymbol in ((CSharpParseOptions)tree.Options).PreprocessorSymbolNames)
        {
            if (definedSymbol == symbolName)
            {
                isDefined = true;
                break;
            }
        }

        // C# requires '#define' and '#undef' to precede the first token in a file. Only active directives
        // apply, so directives inside an inactive '#if' branch must not override the call-site state.
        foreach (SyntaxTrivia trivia in tree.GetRoot().GetLeadingTrivia())
        {
            switch (trivia.GetStructure())
            {
                case DefineDirectiveTriviaSyntax { IsActive: true } define when define.Name.ValueText == symbolName:
                    isDefined = true;
                    break;
                case UndefDirectiveTriviaSyntax { IsActive: true } undef when undef.Name.ValueText == symbolName:
                    isDefined = false;
                    break;
                default:
                    break;
            }
        }

        return isDefined;
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

        // A reference to an operand handed out anywhere in the method (e.g. 'Foo(ref i)') can outlive the call
        // that receives it, so it might be used to modify that operand from a place the scan below can't see
        if (AreOperandsEscaping(GetOperationBlock(statements[readIndex]), target, indexSymbol))
        {
            return false;
        }

        for (int i = writeIndex; i <= readIndex; i++)
        {
            foreach (IOperation operation in statements[i].DescendantsAndSelf())
            {
                // Reassigning the span parameter would make the write apply to an entirely different buffer
                if (IsPotentialWrite(operation, target.Parameter) && !IsUnevaluated(operation, target.Symbols))
                {
                    return false;
                }

                // Likewise, modifying the index variable would make the two accesses target different elements
                if (indexSymbol is not null && IsPotentialWrite(operation, indexSymbol) && !IsUnevaluated(operation, target.Symbols))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Checks whether a reference to the operands a covering write depends on is handed out anywhere in an
    /// operation block (e.g. as a <c>ref</c> argument), which could be used to modify them at any point.
    /// </summary>
    /// <param name="operationBlock">The operation block to inspect.</param>
    /// <param name="target">The location the write covers.</param>
    /// <param name="indexSymbol">The local or parameter used as the index, if there is one.</param>
    /// <returns>Whether a reference to one of the operands might have escaped.</returns>
    private static bool AreOperandsEscaping(IOperation operationBlock, ReadTarget target, ISymbol? indexSymbol)
    {
        foreach (IOperation operation in operationBlock.DescendantsAndSelf())
        {
            // A reference passed to another method can outlive the call that receives it (e.g. by being stored
            // in a 'ref' field, or by being wrapped in a span), so it is not enough to only look at the calls
            // between the write and the read: one made before the write can still be used to modify an operand.
            if (operation is not IArgumentOperation { Parameter.RefKind: not RefKind.None, Value: { } value } ||
                IsUnevaluated(operation, target.Symbols))
            {
                continue;
            }

            // Ordinary 'out' parameters are scoped by default, unlike '[UnscopedRef] out' parameters.
            // Keep treating 'ref' arguments conservatively: APIs such as 'MemoryMarshal.CreateSpan'
            // can return an alias even though their 'ref' parameter is annotated as scoped.
            if (operation is IArgumentOperation { Parameter: { RefKind: RefKind.Out, ScopedKind: ScopedKind.ScopedRef } })
            {
                continue;
            }

            if (MayReferenceOperand(value, target.Parameter) ||
                (indexSymbol is not null && MayReferenceOperand(value, indexSymbol)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the operation block containing a given operation.
    /// </summary>
    /// <param name="operation">The operation to get the operation block for.</param>
    /// <returns>The root operation containing <paramref name="operation"/>.</returns>
    private static IOperation GetOperationBlock(IOperation operation)
    {
        while (operation.Parent is { } parent)
        {
            operation = parent;
        }

        return operation;
    }

    /// <summary>
    /// Checks whether a given operation is a reference to a local or parameter that might modify its value.
    /// </summary>
    /// <param name="operation">The operation to check.</param>
    /// <param name="symbol">The local or parameter to look for.</param>
    /// <returns>Whether <paramref name="operation"/> might modify the value of <paramref name="symbol"/>.</returns>
    private static bool IsPotentialWrite(IOperation operation, ISymbol symbol)
    {
        if (!IsReferenceTo(operation, symbol))
        {
            return false;
        }

        // A ref conditional preserves the storage denoted by either arm. A write to the selected arm
        // can invalidate either operand, even though it is not a definite write to both of them.
        while (operation.Parent is IConditionalOperation { IsRef: true } conditional &&
               !ReferenceEquals(conditional.Condition, operation))
        {
            operation = conditional;
        }

        return operation.Parent switch
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
    /// Checks whether a by-reference argument can refer to an operand, possibly through a ref conditional.
    /// </summary>
    /// <param name="operation">The reference-producing expression to check.</param>
    /// <param name="symbol">The operand to look for.</param>
    /// <returns>Whether <paramref name="operation"/> might reference <paramref name="symbol"/>.</returns>
    private static bool MayReferenceOperand(IOperation operation, ISymbol symbol)
    {
        return IsReferenceTo(operation, symbol) ||
            (operation is IConditionalOperation { IsRef: true } conditional &&
             (MayReferenceOperand(conditional.WhenTrue, symbol) ||
              (conditional.WhenFalse is { } whenFalse && MayReferenceOperand(whenFalse, symbol))));
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
            ISimpleAssignmentOperation { IsRef: false } simpleAssignment => ReferenceEquals(simpleAssignment.Target, operation),

            // 'Foo(out span[i])' or 'Foo(out x)': the callee has to assign the element before returning
            IArgumentOperation { Parameter.RefKind: RefKind.Out } => true,

            ITupleOperation tuple => IsDeconstructionTarget(tuple),

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
    /// <returns>Whether preceding writes can be relied upon for reads in <paramref name="operationBlock"/>.</returns>
    /// <remarks>
    /// Recognizing a preceding write relies on being able to see every write to the span parameter and to the
    /// index variables in source order. That is not possible when the body can jump around, or when it creates
    /// an indirection that could be used to modify a variable from a place the ordered scan can't account for.
    /// </remarks>
    private static bool AllowsSuppression(IOperation operationBlock)
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

                // A writable 'ref' alias, whether declared or reassigned, can be used to modify whatever
                // it points at from anywhere it is in scope. Note that the loop variable of a 'foreach'
                // loop is not a concern here, as its declarator does not have an initializer.
                case IVariableDeclaratorOperation { Symbol.RefKind: RefKind.Ref, Initializer: not null }:
                case ISimpleAssignmentOperation { IsRef: true }:
                    return false;
                default:
                    break;
            }
        }

        return true;
    }

    /// <summary>
    /// The well known symbols used by the analyzer.
    /// </summary>
    /// <param name="spanType">The <see cref="INamedTypeSymbol"/> for <see cref="System.Span{T}"/>.</param>
    /// <param name="readOnlySpanType">The <see cref="INamedTypeSymbol"/> for <see cref="System.ReadOnlySpan{T}"/>.</param>
    /// <param name="conditionalAttributeType">The <see cref="INamedTypeSymbol"/> for <c>[Conditional]</c>, if available.</param>
    /// <param name="referenceAssemblyAttributeType">The marker for reference projection assemblies, if available.</param>
    /// <param name="windowsRuntimeTypeAttributeType">The marker for implementation projection types, if available.</param>
    private readonly struct WellKnownSymbols(
        INamedTypeSymbol spanType,
        INamedTypeSymbol readOnlySpanType,
        INamedTypeSymbol? conditionalAttributeType,
        INamedTypeSymbol? referenceAssemblyAttributeType,
        INamedTypeSymbol? windowsRuntimeTypeAttributeType)
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

        /// <summary>
        /// Gets the marker for reference projection assemblies.
        /// </summary>
        public INamedTypeSymbol? ReferenceAssemblyAttributeType => referenceAssemblyAttributeType;

        /// <summary>
        /// Gets the marker for implementation projection types.
        /// </summary>
        public INamedTypeSymbol? WindowsRuntimeTypeAttributeType => windowsRuntimeTypeAttributeType;
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
        /// <param name="access">The element reference whose containing field path is being read, if any.</param>
        private ReadTarget(IParameterSymbol parameter, WellKnownSymbols symbols, IOperation? index, ILocalSymbol? alias, IOperation? access = null)
        {
            Parameter = parameter;
            Symbols = symbols;
            Index = index;
            Alias = alias;
            Access = access;
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
        /// Gets the element reference whose containing field path is being read, if any.
        /// </summary>
        public IOperation? Access { get; }

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

        /// <summary>
        /// Includes the access operation so field writes can be matched to the value being read.
        /// </summary>
        /// <param name="access">The indexer or loop-variable reference at the root of the read.</param>
        /// <returns>A target with the same location and the specified field-access context.</returns>
        public ReadTarget WithAccess(IOperation access)
        {
            return new(Parameter, Symbols, Index, Alias, access);
        }
    }
}
