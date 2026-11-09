// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace WindowsRuntime.InteropGenerator.Discovery;

/// <summary>
/// Attributes metadata roots to excluded implementations without removing independently used roots.
/// </summary>
internal sealed class InteropAnalysisExclusions
{
    /// <summary>The runtime marker identifying excluded lexical implementations.</summary>
    private const string AttributeName = "WindowsRuntimeExcludeFromInteropAnalysisAttribute";

    /// <summary>The module-owned definitions belonging to excluded lexical implementations.</summary>
    private readonly HashSet<TypeDefinition> _types;

    /// <summary>The lazily attributed records without an independent unexcluded root.</summary>
    private readonly Lazy<HashSet<IMetadataMember>> _roots;

    /// <summary>Creates the read-only index for a module with excluded implementations.</summary>
    /// <param name="module">The loaded input module.</param>
    /// <param name="signatureComparer">The invocation's type identity comparer.</param>
    /// <param name="types">The module-owned excluded definitions.</param>
    /// <param name="token">The cancellation token.</param>
    private InteropAnalysisExclusions(
        ModuleDefinition module,
        SignatureComparer signatureComparer,
        HashSet<TypeDefinition> types,
        CancellationToken token)
    {
        _types = types;
        _roots = new Lazy<HashSet<IMetadataMember>>(
            valueFactory: () => FindExcludedRoots(module, signatureComparer, token),
            mode: LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>Attributes excluded definitions without allocating an index for unannotated modules.</summary>
    /// <param name="module">The loaded input module.</param>
    /// <param name="context">The invocation's resolution context.</param>
    /// <param name="signatureComparer">The invocation's type identity comparer.</param>
    /// <param name="token">The cancellation token.</param>
    /// <param name="exclusions">The read-only index when excluded definitions are present.</param>
    /// <returns>Whether the module declares an excluded lexical implementation.</returns>
    public static bool TryCreate(
        ModuleDefinition module,
        RuntimeContext context,
        SignatureComparer signatureComparer,
        CancellationToken token,
        [NotNullWhen(true)] out InteropAnalysisExclusions? exclusions)
    {
        exclusions = null;
        bool mayReferenceAttribute = module.Assembly?.Name == "WinRT.Runtime" ||
            module.EnumerateTableMembers<TypeReference>(TableIndex.TypeRef).Any(type =>
                type.Name == AttributeName && type.Namespace == "WindowsRuntime.InteropServices");

        if (!mayReferenceAttribute)
        {
            return false;
        }

        HashSet<TypeDefinition>? excludedTypes = null;
        foreach (TypeDefinition type in module.GetAllTypes())
        {
            token.ThrowIfCancellationRequested();

            for (TypeDefinition? owner = type; owner is not null; owner = owner.DeclaringType)
            {
                if (owner.FindCustomAttributes("WindowsRuntime.InteropServices", AttributeName).Any(attribute =>
                    attribute.Type is { } attributeType &&
                    attributeType.TryResolve(context, out TypeDefinition? definition) &&
                    definition.DeclaringModule?.Assembly?.Name == "WinRT.Runtime"))
                {
                    _ = (excludedTypes ??= new(ReferenceEqualityComparer.Instance)).Add(type);
                    break;
                }
            }
        }

        if (excludedTypes is null)
        {
            return false;
        }

        exclusions = new(
            module: module,
            signatureComparer: signatureComparer,
            types: excludedTypes,
            token: token);
        return true;
    }

    /// <summary>Checks whether a type's declared implementation is excluded.</summary>
    /// <param name="type">The resolved type definition.</param>
    /// <returns>Whether the type belongs to an excluded lexical implementation.</returns>
    public bool Contains(TypeDefinition type)
    {
        return _types.Contains(type);
    }

    /// <summary>Checks whether a metadata record is used only by excluded implementations.</summary>
    /// <param name="root">The loaded metadata record.</param>
    /// <returns>Whether the record has no independent unexcluded root.</returns>
    public bool Contains(IMetadataMember root)
    {
        return _roots.Value.Contains(root);
    }

    /// <summary>Finds records attributable only to excluded implementation signatures and bodies.</summary>
    /// <param name="module">The loaded module owning the metadata records.</param>
    /// <param name="signatureComparer">The shared comparer for equivalent inline signatures.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>The excluded records, preserving independent and unattributed roots.</returns>
    private HashSet<IMetadataMember> FindExcludedRoots(
        ModuleDefinition module,
        SignatureComparer signatureComparer,
        CancellationToken token)
    {
        IMetadataMember[] records =
        [
            .. module.EnumerateTableMembers<TypeSpecification>(TableIndex.TypeSpec),
            .. module.EnumerateTableMembers<MethodSpecification>(TableIndex.MethodSpec),
            .. module.EnumerateTableMembers<MemberReference>(TableIndex.MemberRef)
        ];
        Dictionary<IMetadataMember, HashSet<IMetadataMember>> edges = new(ReferenceEqualityComparer.Instance);
        Dictionary<IMetadataMember, HashSet<IMetadataMember>> preservationEdges = new(ReferenceEqualityComparer.Instance);
        Dictionary<TypeSignature, List<TypeSpecification>> signatures = new(signatureComparer);
        HashSet<IMetadataMember> included = new(ReferenceEqualityComparer.Instance);
        HashSet<IMetadataMember> excluded = new(ReferenceEqualityComparer.Instance);

        foreach (IMetadataMember record in records)
        {
            edges.Add(record, new(ReferenceEqualityComparer.Instance));
            preservationEdges.Add(record, new(ReferenceEqualityComparer.Instance));
            if (record is TypeSpecification { Signature: { } signature } specification)
            {
                if (!signatures.TryGetValue(signature, out List<TypeSpecification>? matches))
                {
                    signatures.Add(signature, matches = []);
                }
                matches.Add(specification);
            }
        }

        // Equal inline signatures must preserve a 'TypeSpec' record's independent one-hop eligibility
        void Collect(object? value, HashSet<IMetadataMember> targets, bool preserveEquivalentSignatures = false)
        {
            Queue<object> pending = new();
            HashSet<object> visited = new(ReferenceEqualityComparer.Instance);
            if (value is not null)
            {
                pending.Enqueue(value);
            }
            while (pending.TryDequeue(out object? current))
            {
                token.ThrowIfCancellationRequested();
                if (!visited.Add(current))
                {
                    continue;
                }
                if (current is IMetadataMember metadata && edges.ContainsKey(metadata))
                {
                    _ = targets.Add(metadata);
                    continue;
                }
                switch (current)
                {
                    case TypeSignature signature:
                        if ((preserveEquivalentSignatures || ReferenceEquals(targets, included)) &&
                            signatures.TryGetValue(signature, out List<TypeSpecification>? matches))
                        {
                            targets.UnionWith(matches);
                        }
                        switch (signature)
                        {
                            case GenericInstanceTypeSignature generic:
                                pending.Enqueue(generic.GenericType);
                                foreach (TypeSignature argument in generic.TypeArguments)
                                {
                                    pending.Enqueue(argument);
                                }
                                break;
                            case CustomModifierTypeSignature modifier:
                                pending.Enqueue(modifier.ModifierType);
                                pending.Enqueue(modifier.BaseType);
                                break;
                            case TypeSpecificationSignature wrapper:
                                pending.Enqueue(wrapper.BaseType);
                                break;
                            case TypeDefOrRefSignature named:
                                pending.Enqueue(named.Type);
                                break;
                            case FunctionPointerTypeSignature pointer:
                                pending.Enqueue(pointer.Signature);
                                break;
                            default:
                                break;
                        }
                        break;
                    case MethodSignature method:
                        pending.Enqueue(method.ReturnType);
                        foreach (TypeSignature parameter in method.ParameterTypes.Concat(method.SentinelParameterTypes))
                        {
                            pending.Enqueue(parameter);
                        }
                        break;
                    case PropertySignature property:
                        pending.Enqueue(property.ReturnType);
                        foreach (TypeSignature parameter in property.ParameterTypes)
                        {
                            pending.Enqueue(parameter);
                        }
                        break;
                    case FieldSignature field:
                        pending.Enqueue(field.FieldType);
                        break;
                    case StandAloneSignature standalone:
                        if (standalone.Signature is not null)
                        {
                            pending.Enqueue(standalone.Signature);
                        }
                        break;
                    case IMethodDefOrRef method:
                        if (method.DeclaringType is not null)
                        {
                            pending.Enqueue(method.DeclaringType);
                        }
                        if (method.Signature is not null)
                        {
                            pending.Enqueue(method.Signature);
                        }
                        break;
                    case IFieldDescriptor field:
                        if (field.DeclaringType is not null)
                        {
                            pending.Enqueue(field.DeclaringType);
                        }
                        if (field.Signature is not null)
                        {
                            pending.Enqueue(field.Signature);
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        foreach (IMetadataMember record in records)
        {
            foreach (bool preserve in new[] { false, true })
            {
                HashSet<IMetadataMember> targets = preserve ? preservationEdges[record] : edges[record];
                switch (record)
                {
                    case TypeSpecification specification:
                        Collect(specification.Signature, targets, preserve);
                        break;
                    case MethodSpecification specification:
                        Collect(specification.Method, targets, preserve);
                        foreach (TypeSignature argument in specification.Signature?.TypeArguments ?? [])
                        {
                            Collect(argument, targets, preserve);
                        }
                        break;
                    case MemberReference reference:
                        Collect(reference.DeclaringType, targets, preserve);
                        Collect(reference.Signature, targets, preserve);
                        break;
                    default:
                        break;
                }
            }
        }

        // Attribute constructors remain independent roots even on excluded declarations
        void Attributes(IHasCustomAttribute owner)
        {
            foreach (CustomAttribute attribute in owner.CustomAttributes)
            {
                Collect(attribute.Constructor, included);
            }
        }
        // Generic constraints describe the type's contract rather than its excluded implementation
        void Constraints(IHasGenericParameters owner)
        {
            foreach (GenericParameter parameter in owner.GenericParameters)
            {
                Attributes(parameter);
                foreach (GenericParameterConstraint constraint in parameter.Constraints)
                {
                    Collect(constraint.Constraint, included);
                    Attributes(constraint);
                }
            }
        }

        Attributes(module);
        // Custom attributes can also belong to references, resources, and metadata specifications
        foreach (CustomAttribute attribute in module.EnumerateTableMembers<CustomAttribute>(TableIndex.CustomAttribute))
        {
            Collect(attribute.Constructor, included);
        }
        if (module.Assembly is not null)
        {
            Attributes(module.Assembly);
        }
        foreach (TypeDefinition type in module.GetAllTypes())
        {
            HashSet<IMetadataMember> implementation = Contains(type) ? excluded : included;
            Attributes(type);
            Constraints(type);
            Collect(type.BaseType, included);
            foreach (InterfaceImplementation item in type.Interfaces)
            {
                Collect(item.Interface, included);
                Attributes(item);
            }
            foreach (MethodImplementation item in type.MethodImplementations)
            {
                Collect(item.Declaration, included);
                Collect(item.Body, included);
            }
            foreach (FieldDefinition field in type.Fields)
            {
                Collect(field.Signature, implementation);
                Attributes(field);
            }
            foreach (PropertyDefinition property in type.Properties)
            {
                Collect(property.Signature, implementation);
                Attributes(property);
            }
            foreach (EventDefinition item in type.Events)
            {
                Collect(item.EventType, implementation);
                Attributes(item);
            }
            foreach (MethodDefinition method in type.Methods)
            {
                Collect(method.Signature, implementation);
                Attributes(method);
                Constraints(method);
                foreach (ParameterDefinition parameter in method.ParameterDefinitions)
                {
                    Attributes(parameter);
                }
                if (method.CilMethodBody is not { } body)
                {
                    continue;
                }
                foreach (CilLocalVariable local in body.LocalVariables)
                {
                    Collect(local.VariableType, implementation);
                }
                foreach (CilExceptionHandler handler in body.ExceptionHandlers)
                {
                    Collect(handler.ExceptionType, implementation);
                }
                foreach (CilInstruction instruction in body.Instructions)
                {
                    Collect(instruction.Operand, implementation);
                }
            }
        }

        void Close(HashSet<IMetadataMember> roots, Dictionary<IMetadataMember, HashSet<IMetadataMember>> dependencies)
        {
            Queue<IMetadataMember> pending = new(roots);
            while (pending.TryDequeue(out IMetadataMember? record))
            {
                token.ThrowIfCancellationRequested();
                foreach (IMetadataMember target in dependencies[record])
                {
                    if (roots.Add(target))
                    {
                        pending.Enqueue(target);
                    }
                }
            }
        }

        Close(excluded, edges);
        // Unattributed/orphan records retain the legacy explicit-root behavior
        included.UnionWith(records.Where(record => !excluded.Contains(record)));
        Close(included, preservationEdges);
        excluded.ExceptWith(included);
        return excluded;
    }
}
