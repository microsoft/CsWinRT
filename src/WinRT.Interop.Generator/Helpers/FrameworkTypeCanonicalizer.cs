// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.Generator;
using WindowsRuntime.Generator.References;
using WindowsRuntime.InteropGenerator.Errors;

namespace WindowsRuntime.InteropGenerator.Helpers;

/// <summary>
/// Rewrites equivalent framework types to their declaring identities in the target reference assemblies.
/// </summary>
internal sealed class FrameworkTypeCanonicalizer : ITypeSignatureVisitor<TypeSignature>
{
    /// <summary>The context used to prove that reference and implementation types resolve identically.</summary>
    private readonly RuntimeContext _runtimeContext;

    /// <summary>The candidate public reference identities, indexed by complete (including nested) type name.</summary>
    private readonly FrozenDictionary<string, TypeReference[]> _referencesByName;

    /// <summary>The canonical reference, if available, for each resolved framework type.</summary>
    private readonly ConcurrentDictionary<TypeDefinition, TypeReference?> _canonicalTypes = new(SignatureComparer.IgnoreVersion);

    /// <summary>
    /// Creates a canonicalizer for the target framework of a generator invocation.
    /// </summary>
    /// <param name="runtimeContext">The implementation resolution context.</param>
    /// <param name="referenceModules">The framework reference modules for the target application.</param>
    /// <param name="contextModule">The application module anchoring the target corlib references.</param>
    public FrameworkTypeCanonicalizer(RuntimeContext runtimeContext, IEnumerable<ModuleDefinition> referenceModules, ModuleDefinition contextModule)
    {
        _runtimeContext = runtimeContext;

        // Import once before parallel discovery. Importing into each input module while another thread
        // enumerates its assembly references would mutate those collections during enumeration.
        CorLibTypeFactory = new(runtimeContext.TargetRuntime.GetDefaultCorLib().ImportWith(contextModule.DefaultImporter));

        Dictionary<string, List<TypeReference>> references = new(StringComparer.Ordinal);

        foreach (ModuleDefinition module in referenceModules)
        {
            AssemblyDefinition definition = module.Assembly!;
            AssemblyReference assembly = new(definition.Name, definition.Version, false, definition.GetPublicKeyToken())
            {
                Culture = definition.Culture
            };

            foreach (TypeDefinition type in module.GetAllTypes())
            {
                if (type.IsModuleType)
                {
                    continue;
                }

                if (!references.TryGetValue(type.FullName, out List<TypeReference>? candidates))
                {
                    candidates = [];
                    references.Add(type.FullName, candidates);
                }

                candidates.Add(CreateReference(type, assembly));
            }
        }

        _referencesByName = references.ToFrozenDictionary(
            static pair => pair.Key,
            static pair => pair.Value.OrderBy(static type => type.Scope!.GetAssembly()!.FullName, StringComparer.Ordinal).ToArray(),
            StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets the corlib type factory for the target framework, independent of each input module's framework.
    /// </summary>
    public CorLibTypeFactory CorLibTypeFactory { get; }

    /// <summary>
    /// Canonicalizes a complete signature without changing the input metadata.
    /// </summary>
    /// <param name="signature">The signature to canonicalize.</param>
    /// <returns>The canonical signature, preserving its generic arguments and wrappers.</returns>
    public TypeSignature Canonicalize(TypeSignature signature)
    {
        return signature.AcceptVisitor(this);
    }

    /// <inheritdoc/>
    public TypeSignature VisitGenericInstanceType(GenericInstanceTypeSignature signature)
    {
        ITypeDefOrRef genericType = CanonicalizeType(signature.GenericType);
        TypeSignature[]? arguments = null;

        for (int i = 0; i < signature.TypeArguments.Count; i++)
        {
            TypeSignature argument = Canonicalize(signature.TypeArguments[i]);

            if (!ReferenceEquals(argument, signature.TypeArguments[i]))
            {
                arguments ??= [.. signature.TypeArguments];
                arguments[i] = argument;
            }
        }

        return ReferenceEquals(genericType, signature.GenericType) && arguments is null
            ? signature
            : new GenericInstanceTypeSignature(genericType, signature.IsValueType, arguments is null ? signature.TypeArguments : arguments);
    }

    /// <inheritdoc/>
    public TypeSignature VisitTypeDefOrRef(TypeDefOrRefSignature signature)
    {
        ITypeDefOrRef type = CanonicalizeType(signature.Type);

        return ReferenceEquals(type, signature.Type)
            ? signature
            : CorLibTypeFactory.FromType(type) ?? type.ToTypeSignature(signature.IsValueType);
    }

    /// <inheritdoc/>
    public TypeSignature VisitCorLibType(CorLibTypeSignature signature)
    {
        return CorLibTypeFactory.FromElementType(signature.ElementType)!;
    }

    /// <inheritdoc/>
    public TypeSignature VisitSzArrayType(SzArrayTypeSignature signature)
    {
        TypeSignature element = Canonicalize(signature.BaseType);
        return ReferenceEquals(element, signature.BaseType) ? signature : new SzArrayTypeSignature(element);
    }

    /// <inheritdoc/>
    public TypeSignature VisitArrayType(ArrayTypeSignature signature)
    {
        TypeSignature element = Canonicalize(signature.BaseType);
        return ReferenceEquals(element, signature.BaseType) ? signature : new ArrayTypeSignature(element, [.. signature.Dimensions]);
    }

    /// <inheritdoc/>
    public TypeSignature VisitBoxedType(BoxedTypeSignature signature)
    {
        TypeSignature element = Canonicalize(signature.BaseType);
        return ReferenceEquals(element, signature.BaseType) ? signature : new BoxedTypeSignature(element);
    }

    /// <inheritdoc/>
    public TypeSignature VisitByReferenceType(ByReferenceTypeSignature signature)
    {
        TypeSignature element = Canonicalize(signature.BaseType);
        return ReferenceEquals(element, signature.BaseType) ? signature : new ByReferenceTypeSignature(element);
    }

    /// <inheritdoc/>
    public TypeSignature VisitCustomModifierType(CustomModifierTypeSignature signature)
    {
        TypeSignature element = Canonicalize(signature.BaseType);
        ITypeDefOrRef modifier = CanonicalizeType(signature.ModifierType);
        return ReferenceEquals(element, signature.BaseType) && ReferenceEquals(modifier, signature.ModifierType)
            ? signature
            : new CustomModifierTypeSignature(modifier, signature.IsRequired, element);
    }

    /// <inheritdoc/>
    public TypeSignature VisitGenericParameter(GenericParameterSignature signature)
    {
        return signature;
    }

    /// <inheritdoc/>
    public TypeSignature VisitPinnedType(PinnedTypeSignature signature)
    {
        TypeSignature element = Canonicalize(signature.BaseType);
        return ReferenceEquals(element, signature.BaseType) ? signature : new PinnedTypeSignature(element);
    }

    /// <inheritdoc/>
    public TypeSignature VisitPointerType(PointerTypeSignature signature)
    {
        TypeSignature element = Canonicalize(signature.BaseType);
        return ReferenceEquals(element, signature.BaseType) ? signature : new PointerTypeSignature(element);
    }

    /// <inheritdoc/>
    public TypeSignature VisitSentinelType(SentinelTypeSignature signature)
    {
        return signature;
    }

    /// <inheritdoc/>
    public TypeSignature VisitFunctionPointerType(FunctionPointerTypeSignature signature)
    {
        MethodSignature method = signature.Signature;
        MethodSignature canonical = new(method.Attributes, Canonicalize(method.ReturnType), method.ParameterTypes.Select(Canonicalize))
        {
            GenericParameterCount = method.GenericParameterCount,
            IncludeSentinel = method.IncludeSentinel
        };

        foreach (TypeSignature parameter in method.SentinelParameterTypes)
        {
            canonical.SentinelParameterTypes.Add(Canonicalize(parameter));
        }

        return new FunctionPointerTypeSignature(canonical);
    }

    /// <summary>
    /// Gets a canonical reference only after proving framework ownership and resolved identity.
    /// </summary>
    /// <param name="type">The original type.</param>
    /// <returns>The canonical reference, or the original type if no target reference matches.</returns>
    public ITypeDefOrRef CanonicalizeType(ITypeDefOrRef type)
    {
        return BaseClassLibraryIdentity.IsBaseClassLibraryPublicKeyToken(type.Scope?.GetAssembly()?.GetPublicKeyToken()) &&
            _referencesByName.ContainsKey(type.FullName) &&
            type.TryResolve(_runtimeContext, out TypeDefinition? definition)
            ? _canonicalTypes.GetOrAdd(definition, GetCanonicalReference) ?? type
            : type;
    }

    /// <summary>
    /// Finds the unambiguous target reference declaration for a resolved framework type.
    /// </summary>
    /// <param name="definition">The resolved type.</param>
    /// <returns>The canonical reference, if one resolves to the same definition.</returns>
    private TypeReference? GetCanonicalReference(TypeDefinition definition)
    {
        TypeReference? canonical = null;

        foreach (TypeReference candidate in _referencesByName[definition.FullName])
        {
            // Names only select candidates. Resolution, including the assembly identity, proves equivalence.
            if (!candidate.TryResolve(_runtimeContext, out TypeDefinition? resolved) ||
                !SignatureComparer.IgnoreVersion.Equals(definition, resolved))
            {
                continue;
            }

            if (canonical is not null)
            {
                if (!SignatureComparer.IgnoreVersion.Equals(canonical, candidate))
                {
                    throw WellKnownInteropExceptions.AmbiguousFrameworkTypeReference(definition, canonical, candidate);
                }

                if (candidate.Scope!.GetAssembly()!.Version <= canonical.Scope!.GetAssembly()!.Version)
                {
                    continue;
                }
            }

            canonical = candidate;
        }

        // A reference-module context would bypass forwarding through that assembly in the implementation graph.
        return canonical is not null ? WithContextModule(canonical, definition.DeclaringModule!) : null;
    }

    /// <summary>
    /// Creates an assembly-scoped reference without binding it to reference-assembly metadata.
    /// </summary>
    /// <param name="type">The reference declaration.</param>
    /// <param name="assembly">The declaring public assembly.</param>
    /// <returns>A reference that the implementation context can resolve, including any declaring types.</returns>
    private static TypeReference CreateReference(TypeDefinition type, AssemblyReference assembly)
    {
        return type.DeclaringType is { } declaringType
            ? CreateReference(declaringType, assembly).CreateTypeReference(type.Name!)
            : assembly.CreateTypeReference(type.Namespace!, type.Name!);
    }

    /// <summary>
    /// Preserves the public scope while supplying an implementation context for metadata assignability.
    /// </summary>
    /// <param name="type">The public reference.</param>
    /// <param name="module">The module containing the resolved implementation.</param>
    /// <returns>The reference with a usable implementation context.</returns>
    private static TypeReference WithContextModule(TypeReference type, ModuleDefinition module)
    {
        IResolutionScope? scope = type.Scope is TypeReference declaringType ? WithContextModule(declaringType, module) : type.Scope;
        return new TypeReference(module, scope, type.Namespace, type.Name);
    }
}
