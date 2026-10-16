// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Serialized;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.Generator.Errors;
using WindowsRuntime.InteropGenerator.Errors;
using WindowsRuntime.InteropGenerator.Models;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <summary>
/// Fingerprints the inputs consumed by emission, independently of application method bodies and MVIDs.
/// </summary>
internal sealed class InteropGenerationFingerprint
{
    /// <summary>The invocation being fingerprinted.</summary>
    private readonly InteropGeneratorArgs _args;

    /// <summary>The completed discovery state.</summary>
    private readonly InteropGeneratorDiscoveryState _state;

    /// <summary>The modules explicitly loaded by discovery.</summary>
    private readonly HashSet<ModuleDefinition> _inputModules;

    /// <summary>Cached, assembly-qualified names, including the scopes of nested generic arguments.</summary>
    private readonly Dictionary<TypeSignature, string> _typeNames = new(ReferenceEqualityComparer.Instance);

    /// <summary>Shares names across equivalent signatures coming from different input modules.</summary>
    private readonly HashSet<string> _uniqueTypeNames = new(StringComparer.Ordinal);

    /// <summary>Cached assembly identities, shared by the many signatures in each module.</summary>
    private readonly Dictionary<AssemblyDescriptor, string> _assemblyNames = new(ReferenceEqualityComparer.Instance);

    /// <summary>Cached names for definitions inspected more than once.</summary>
    private readonly Dictionary<TypeDefinition, string> _definitionNames = new(ReferenceEqualityComparer.Instance);

    /// <summary>Signatures whose resolved metadata is also an emission dependency.</summary>
    private readonly Queue<TypeSignature> _pendingTypes = new();

    /// <summary>Definitions already visited while expanding the dependency graph.</summary>
    private readonly HashSet<TypeDefinition> _definitions = new(ReferenceEqualityComparer.Instance);

    /// <summary>Modules whose implementation details are consumed, including IL and RVA data.</summary>
    private readonly HashSet<ModuleDefinition> _implementationModules = new(ReferenceEqualityComparer.Instance);

    /// <summary>
    /// Creates a fingerprint builder for an invocation.
    /// </summary>
    /// <param name="args">The invocation arguments.</param>
    /// <param name="state">The completed discovery state.</param>
    private InteropGenerationFingerprint(InteropGeneratorArgs args, InteropGeneratorDiscoveryState state)
    {
        _args = args;
        _state = state;
        _inputModules = new(state.Modules.Values, ReferenceEqualityComparer.Instance);

        // Emission consumes implementation details from the runtime, framework, and reference projection modules
        foreach (ModuleDefinition module in state.Modules.Values)
        {
            if (module.IsWindowsRuntimeModule ||
                module.IsBaseClassLibraryModule ||
                module.Assembly is { IsWindowsRuntimeReferenceAssembly: true })
            {
                _ = _implementationModules.Add(module);
            }
        }

        // The same applies to all private projection and component modules
        foreach (ModuleDefinition? module in (ReadOnlySpan<ModuleDefinition?>)
        [
            state.WindowsRuntimeSdkProjectionModule,
            state.WindowsRuntimeSdkXamlProjectionModule,
            state.WindowsRuntimeProjectionModule,
            state.WindowsRuntimeComponentModule
        ])
        {
            if (module is not null)
            {
                _ = _implementationModules.Add(module);
            }
        }
    }

    /// <summary>
    /// Computes a cache key from the inputs consumed by emission.
    /// </summary>
    /// <param name="args">The invocation arguments.</param>
    /// <param name="state">The completed discovery state.</param>
    /// <returns>The SHA256 fingerprint.</returns>
    public static byte[] Create(InteropGeneratorArgs args, InteropGeneratorDiscoveryState state)
    {
        try
        {
            return new InteropGenerationFingerprint(args, state).Create();
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw WellKnownInteropExceptions.GenerationFingerprintError(e);
        }
    }

    /// <summary>
    /// Hashes the discovery roots and their metadata dependencies in a deterministic order.
    /// </summary>
    /// <returns>The resulting SHA256 fingerprint.</returns>
    private byte[] Create()
    {
        using SHA256 hash = SHA256.Create();

        using (CryptoStream crypto = new(Stream.Null, hash, CryptoStreamMode.Write))
        using (BufferedStream buffer = new(crypto, 16384))
        using (BinaryWriter writer = new(buffer, Encoding.UTF8))
        {
            writer.Write("CsWinRT.Interop.EmissionInputs.1");

            WriteGeneratorIdentity(writer);
            WriteOptions(writer);
            WriteModules(writer);
            WriteTypeHierarchy(writer);
            WriteDiscoveredTypes(writer);
            WriteUserDefinedVtableSets(writer);

            // Expand the metadata dependencies only after all discovered roots have been enqueued
            DiscoverMetadataDependencies();

            WriteMetadataDependencies(writer);
            WriteImplementationModules(writer);
        }

        return hash.Hash!;
    }

    /// <summary>
    /// Includes the actual generator build, not just its public assembly version.
    /// </summary>
    /// <param name="writer">The writer for the fingerprint.</param>
    private static void WriteGeneratorIdentity(BinaryWriter writer)
    {
        writer.Write(Environment.Version.ToString());

        using FileStream stream = File.OpenRead(Environment.ProcessPath!);

        writer.Write(SHA256.HashData(stream));
    }

    /// <summary>
    /// Writes the invocation options that can affect emission.
    /// </summary>
    /// <param name="writer">The writer for the fingerprint.</param>
    private void WriteOptions(BinaryWriter writer)
    {
        writer.Write(_state.RuntimeContext.TargetRuntime.ToString());
        writer.Write(_state.Modules[_args.OutputAssemblyPath].Assembly?.Version.ToString() ?? "");
        writer.Write(_args.UseWindowsUIXamlProjections);
        writer.Write(_args.EnableDefaultCustomPropertyProviderSupport);
        writer.Write((int)_args.MarshallingMode);
        writer.Write(_args.AnalyzeNetStandardAssemblies);
        writer.Write(_state.MarshallingEnabledAssemblyNames.Count);

        foreach (string name in _state.MarshallingEnabledAssemblyNames.Order(StringComparer.OrdinalIgnoreCase))
        {
            writer.Write(name);
        }

        writer.Write(_state.MarshallingDisabledAssemblyNames.Count);

        foreach (string name in _state.MarshallingDisabledAssemblyNames.Order(StringComparer.OrdinalIgnoreCase))
        {
            writer.Write(name);
        }

        writer.Write(_args.GenerateCollectionChangedListVtables);
        writer.Write(_args.ValidateWinRTRuntimeAssemblyVersion);
        writer.Write(_args.ValidateWinRTRuntimeDllVersion2References);
        writer.Write(_args.TreatWarningsAsErrors);
    }

    /// <summary>
    /// Writes the identities and assembly references of all loaded modules.
    /// </summary>
    /// <param name="writer">The writer for the fingerprint.</param>
    private void WriteModules(BinaryWriter writer)
    {
        writer.Write(_state.Modules.Count);

        foreach (ModuleDefinition module in _state.Modules.Values.OrderByFullyQualifiedName())
        {
            _args.Token.ThrowIfCancellationRequested();

            WriteModuleIdentity(writer, module);

            writer.Write(module.ReferencesWindowsRuntimeAssembly);
            writer.Write(module.IsWindowsRuntimeModule);
            writer.Write(module.Assembly is { IsWindowsRuntimeComponentAssembly: true });

            // Imported references retain binding flags and versions, even though discovery
            // deliberately ignores versions when deduplicating constructed types.
            writer.Write(module.AssemblyReferences.Count);

            foreach (AssemblyReference reference in module.AssemblyReferences.OrderBy(static reference => reference.FullName, StringComparer.Ordinal))
            {
                writer.Write(reference.FullName);
                writer.Write((uint)reference.Attributes);
            }
        }
    }

    /// <summary>
    /// Writes the discovered type hierarchy entries.
    /// </summary>
    /// <param name="writer">The writer for the fingerprint.</param>
    private void WriteTypeHierarchy(BinaryWriter writer)
    {
        writer.Write(_state.TypeHierarchyEntries.Count);

        foreach ((string name, string baseName) in _state.TypeHierarchyEntries.OrderBy(static entry => entry.Key, StringComparer.Ordinal))
        {
            writer.Write(name);
            writer.Write(baseName);
        }
    }

    /// <summary>
    /// Writes all discovered types to emit, and enqueues them as roots for their metadata dependencies.
    /// </summary>
    /// <param name="writer">The writer for the fingerprint.</param>
    private void WriteDiscoveredTypes(BinaryWriter writer)
    {
        WriteTypes(writer, _state.GenericDelegateTypes);
        WriteTypes(writer, _state.IEnumerator1Types);
        WriteTypes(writer, _state.IEnumerable1Types);
        WriteTypes(writer, _state.IReadOnlyList1Types);
        WriteTypes(writer, _state.IList1Types);
        WriteTypes(writer, _state.IReadOnlyDictionary2Types);
        WriteTypes(writer, _state.IDictionary2Types);
        WriteTypes(writer, _state.KeyValuePairTypes);
        WriteTypes(writer, _state.IMapChangedEventArgs1Types);
        WriteTypes(writer, _state.IObservableVector1Types);
        WriteTypes(writer, _state.IObservableMap2Types);
        WriteTypes(writer, _state.IAsyncActionWithProgress1Types);
        WriteTypes(writer, _state.IAsyncOperation1Types);
        WriteTypes(writer, _state.IAsyncOperationWithProgress2Types);
        WriteTypeMappings(writer, _state.SzArrayAndVtableTypes);
        WriteTypeMappings(writer, _state.UserDefinedAndVtableTypes);
    }

    /// <summary>
    /// Writes the distinct interface sets of user-defined types, and enqueues their interfaces as roots.
    /// </summary>
    /// <param name="writer">The writer for the fingerprint.</param>
    private void WriteUserDefinedVtableSets(BinaryWriter writer)
    {
        string[][] vtableSets = new string[_state.UserDefinedVtableTypes.Count][];
        int index = 0;

        // Do not use the emit set comparer here: it repeatedly formats and sorts signatures
        // inside each comparison. Materialize each name once for this additional pass.
        foreach (TypeSignatureEquatableSet types in _state.UserDefinedVtableTypes)
        {
            foreach (TypeSignature type in types)
            {
                _pendingTypes.Enqueue(type);
            }

            vtableSets[index++] = [.. types.Select(GetTypeName).Order(StringComparer.Ordinal)];
        }

        Array.Sort(vtableSets, static (left, right) => ((ReadOnlySpan<string>)left).CompareLexicographically(right));

        writer.Write(vtableSets.Length);

        foreach (string[] types in vtableSets)
        {
            writer.Write(types.Length);

            foreach (string type in types)
            {
                writer.Write(type);
            }
        }
    }

    /// <summary>
    /// Writes the metadata of all type definitions that emission depends on.
    /// </summary>
    /// <param name="writer">The writer for the fingerprint.</param>
    private void WriteMetadataDependencies(BinaryWriter writer)
    {
        writer.Write(_definitions.Count);

        foreach (TypeDefinition definition in _definitions
            .OrderBy(GetDefinitionName, StringComparer.Ordinal)
            .ThenBy(static type => type.DeclaringModule?.FilePath, StringComparer.Ordinal))
        {
            _args.Token.ThrowIfCancellationRequested();

            WriteTypeDefinition(writer, definition);
        }
    }

    /// <summary>
    /// Writes the identities and full contents of all modules whose implementation details emission consumes.
    /// </summary>
    /// <param name="writer">The writer for the fingerprint.</param>
    private void WriteImplementationModules(BinaryWriter writer)
    {
        ModuleDefinition[] implementationModules = [.. _implementationModules
            .OrderBy(static module => $"{module.Name}[{module.Assembly}]", StringComparer.Ordinal)
            .ThenBy(static module => module.FilePath, StringComparer.Ordinal)];

        byte[][] implementationHashes = new byte[implementationModules.Length][];

        // Hash the files in parallel, as some of them (e.g. the Windows SDK projection) are quite large
        _ = Parallel.For(
            fromInclusive: 0,
            toExclusive: implementationModules.Length,
            parallelOptions: new ParallelOptions { CancellationToken = _args.Token, MaxDegreeOfParallelism = _args.MaxDegreesOfParallelism },
            body: i =>
            {
                using FileStream stream = File.OpenRead(implementationModules[i].FilePath!);

                implementationHashes[i] = SHA256.HashData(stream);
            });

        writer.Write(implementationModules.Length);

        for (int i = 0; i < implementationModules.Length; i++)
        {
            _args.Token.ThrowIfCancellationRequested();

            WriteModuleIdentity(writer, implementationModules[i]);

            // Projection IID getters read RVA data. Runtime marshaller signatures and metadata
            // lookups can change without changing the discovered set, so names/MVIDs are not enough.
            writer.Write(implementationHashes[i]);
        }
    }

    /// <summary>
    /// Writes a module's binding identity and the name used by IgnoresAccessChecksTo.
    /// </summary>
    /// <param name="writer">The writer for the fingerprint.</param>
    /// <param name="module">The module to write.</param>
    private void WriteModuleIdentity(BinaryWriter writer, ModuleDefinition module)
    {
        writer.Write(module.Name?.Value ?? "");
        writer.Write(GetAssemblyName(module.Assembly));
        writer.Write((uint)(module.Assembly?.Attributes ?? 0));
    }

    /// <summary>
    /// Writes an unordered set of discovered types with explicit collection boundaries.
    /// </summary>
    /// <typeparam name="T">The type of signatures to write.</typeparam>
    /// <param name="writer">The writer for the fingerprint.</param>
    /// <param name="types">The discovered types to write.</param>
    private void WriteTypes<T>(BinaryWriter writer, IReadOnlyCollection<T> types)
        where T : TypeSignature
    {
        writer.Write(types.Count);

        foreach (string name in types.Select(GetTypeName).Order(StringComparer.Ordinal))
        {
            writer.Write(name);
        }

        foreach (T type in types)
        {
            _pendingTypes.Enqueue(type);
        }
    }

    /// <summary>
    /// Writes a type-to-vtable mapping without relying on concurrent dictionary iteration order.
    /// </summary>
    /// <typeparam name="T">The type of signatures to write.</typeparam>
    /// <param name="writer">The writer for the fingerprint.</param>
    /// <param name="types">The discovered types and their interface sets to write.</param>
    private void WriteTypeMappings<T>(BinaryWriter writer, IReadOnlyDictionary<T, TypeSignatureEquatableSet> types)
        where T : TypeSignature
    {
        writer.Write(types.Count);

        foreach ((T type, TypeSignatureEquatableSet interfaces) in types.OrderBy(pair => GetTypeName(pair.Key), StringComparer.Ordinal))
        {
            writer.Write(GetTypeName(type));

            _pendingTypes.Enqueue(type);

            WriteTypes(writer, interfaces);
        }
    }

    /// <summary>
    /// Expands only metadata reachable from emission roots, not every application type.
    /// </summary>
    private void DiscoverMetadataDependencies()
    {
        // Visit each signature object once to terminate recursive type graphs
        HashSet<TypeSignature> visited = new(ReferenceEqualityComparer.Instance);

        while (_pendingTypes.TryDequeue(out TypeSignature? signature))
        {
            _args.Token.ThrowIfCancellationRequested();

            if (!visited.Add(signature))
            {
                continue;
            }

            // Resolving a constructed or wrapped type alone would lose its argument or element dependencies
            if (signature is GenericInstanceTypeSignature generic)
            {
                foreach (TypeSignature argument in generic.TypeArguments)
                {
                    _pendingTypes.Enqueue(argument);
                }
            }
            else if (signature is TypeSpecificationSignature specification)
            {
                _pendingTypes.Enqueue(specification.BaseType);
            }

            // Different signatures can resolve to the same definition; inspect its metadata only once
            if (signature is GenericParameterSignature or FunctionPointerTypeSignature or SentinelTypeSignature ||
                !signature.TryResolve(_state.RuntimeContext, out TypeDefinition? definition) ||
                !_definitions.Add(definition))
            {
                continue;
            }

            ModuleDefinition declaringModule = definition.DeclaringModule!;

            // Hash whole modules when emission consumes projection code or dependencies outside the explicit inputs
            if (!_implementationModules.Contains(declaringModule) &&
                (definition.IsProjectedWindowsRuntimeType || !_inputModules.Contains(declaringModule)))
            {
                _ = _implementationModules.Add(declaringModule);
            }

            // Inherited, enclosing, and implemented types contribute metadata used to build marshalling support
            if (definition.BaseType is { } baseType)
            {
                _pendingTypes.Enqueue(baseType.ToTypeSignature(_state.RuntimeContext));
            }

            if (definition.DeclaringType is { } declaringType)
            {
                _pendingTypes.Enqueue(declaringType.ToTypeSignature());
            }

            foreach (InterfaceImplementation implementation in definition.Interfaces)
            {
                if (implementation.Interface is { } interfaceType)
                {
                    _pendingTypes.Enqueue(interfaceType.ToTypeSignature(_state.RuntimeContext));
                }
            }

            // Field order and recursively nested value types affect signatures, blittability,
            // and disposal. Ordinary CCW class fields and all application method bodies do not.
            if (definition.IsValueType)
            {
                foreach (FieldDefinition field in definition.Fields)
                {
                    if (!field.IsStatic && field.Signature is { } fieldSignature)
                    {
                        _pendingTypes.Enqueue(fieldSignature.FieldType);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Writes the metadata that emission reads from a discovered application type.
    /// </summary>
    /// <param name="writer">The writer for the fingerprint.</param>
    /// <param name="type">The type definition to write.</param>
    private void WriteTypeDefinition(BinaryWriter writer, TypeDefinition type)
    {
        bool isImplementationModuleType = _implementationModules.Contains(type.DeclaringModule!);

        writer.Write(GetDefinitionName(type));
        WriteModuleIdentity(writer, type.DeclaringModule!);
        writer.Write(isImplementationModuleType);

        // The full contents of implementation modules are already part of the fingerprint
        if (isImplementationModuleType)
        {
            return;
        }

        // Type flags, inheritance, and layout affect the marshalling code generated for application types
        writer.Write((uint)type.Attributes);
        writer.Write(type.BaseType is { } baseType ? GetTypeName(baseType.ToTypeSignature(_state.RuntimeContext)) : "");
        writer.Write(type.ClassLayout?.ClassSize ?? 0);
        writer.Write(type.ClassLayout?.PackingSize ?? 0);
        writer.Write(type.Interfaces.Count);

        // Preserve interface declaration order as well as identity, since emission can select a representative interface
        foreach (InterfaceImplementation implementation in type.Interfaces)
        {
            writer.Write(implementation.Interface is { } interfaceType ? GetTypeName(interfaceType.ToTypeSignature(_state.RuntimeContext)) : "");
        }

        // Generic parameter flags include variance, which affects the interfaces exposed by a CCW
        writer.Write(type.GenericParameters.Count);

        foreach (GenericParameter parameter in type.GenericParameters)
        {
            writer.Write((int)parameter.Attributes);
        }

        // Attribute identities and payloads can alter runtime class names and marshalling behavior
        WriteCustomAttributes(writer, type);

        writer.Write(type.IsValueType);

        // Instance field order, types, and offsets determine value-type signatures and ABI layout
        if (type.IsValueType)
        {
            FieldDefinition[] fields = [.. type.Fields.Where(static field => !field.IsStatic)];

            writer.Write(fields.Length);

            foreach (FieldDefinition field in fields)
            {
                writer.Write(field.Name?.Value ?? "");
                writer.Write((int)field.Attributes);
                writer.Write(field.Signature is { } signature ? GetTypeName(signature.FieldType) : "");
                writer.Write(field.FieldOffset ?? 0);
            }
        }
    }

    /// <summary>
    /// Writes attribute identities and payloads without parsing unrelated user-defined attributes.
    /// </summary>
    /// <param name="writer">The writer for the fingerprint.</param>
    /// <param name="type">The type definition whose attributes to write.</param>
    private void WriteCustomAttributes(BinaryWriter writer, TypeDefinition type)
    {
        writer.Write(type.CustomAttributes.Count);

        if (type.CustomAttributes.Count == 0)
        {
            return;
        }

        if (type.DeclaringModule is not SerializedModuleDefinition module)
        {
            throw new NotSupportedException("A type metadata dependency has no serialized module.");
        }

        TablesStream tables = module.DotNetDirectory.Metadata!.GetStream<TablesStream>()!;
        BlobStream blobs = module.DotNetDirectory.Metadata.GetStream<BlobStream>()!;

        foreach (CustomAttribute attribute in type.CustomAttributes)
        {
            writer.Write(attribute.Type is { } attributeType ? GetTypeName(attributeType.ToTypeSignature(_state.RuntimeContext)) : "");
            writer.Write(attribute.Constructor?.FullName ?? "");

            CustomAttributeRow row = tables.GetTable<CustomAttributeRow>().GetByRid(attribute.MetadataToken.Rid);
            byte[] blob = blobs.GetBlobByIndex(row.Value) ?? throw new BadImageFormatException("An attribute dependency has an invalid blob.");

            writer.Write(blob.Length);
            writer.Write(blob);
        }
    }

    /// <summary>
    /// Formats each signature once, rather than repeatedly formatting it in sort comparisons.
    /// </summary>
    /// <param name="type">The signature to format.</param>
    /// <returns>The unambiguous, assembly-qualified name of <paramref name="type"/>.</returns>
    private string GetTypeName(TypeSignature type)
    {
        // Reuse names by signature object to avoid rebuilding them during dependency traversal and sorting
        if (!_typeNames.TryGetValue(type, out string? name))
        {
            DefaultInterpolatedStringHandler builder = new(0, 0, CultureInfo.InvariantCulture);

            // Prefix the signature kind so wrappers and their underlying types cannot share a key
            builder.AppendFormatted((int)type.ElementType);
            builder.AppendLiteral(":");

            switch (type)
            {
                // Include the generic definition and recursively scoped arguments, not just their display names
                case GenericInstanceTypeSignature generic:
                    builder.AppendFormatted(generic.IsValueType);
                    AppendDescriptor(ref builder, generic.GenericType);
                    builder.AppendFormatted(generic.TypeArguments.Count);
                    builder.AppendLiteral(":");

                    foreach (TypeSignature argument in generic.TypeArguments)
                    {
                        AppendPart(ref builder, GetTypeName(argument));
                    }

                    break;

                // Preserve modifier identity and array bounds in addition to the wrapped element type
                case TypeSpecificationSignature specification:
                    if (specification is CustomModifierTypeSignature modifier)
                    {
                        builder.AppendFormatted(modifier.IsRequired);
                        AppendDescriptor(ref builder, modifier.ModifierType);
                    }
                    else if (specification is ArrayTypeSignature array)
                    {
                        builder.AppendFormatted(array.Dimensions.Count);
                        builder.AppendLiteral(":");

                        foreach (ArrayDimension dimension in array.Dimensions)
                        {
                            AppendPart(ref builder, dimension.Size?.ToString(CultureInfo.InvariantCulture));
                            AppendPart(ref builder, dimension.LowerBound?.ToString(CultureInfo.InvariantCulture));
                        }
                    }

                    AppendPart(ref builder, GetTypeName(specification.BaseType));
                    break;

                // Type and method parameters can have the same index but represent different slots
                case GenericParameterSignature parameter:
                    builder.AppendFormatted((int)parameter.ParameterType);
                    builder.AppendLiteral(":");
                    builder.AppendFormatted(parameter.Index);
                    break;

                // Function pointer identity includes calling convention, return type, and parameter signatures
                case FunctionPointerTypeSignature pointer:
                    builder.AppendFormatted((int)pointer.Signature.Attributes);
                    builder.AppendLiteral(":");
                    builder.AppendFormatted(pointer.Signature.GenericParameterCount);
                    builder.AppendLiteral(":");
                    AppendPart(ref builder, GetTypeName(pointer.Signature.ReturnType));
                    builder.AppendFormatted(pointer.Signature.ParameterTypes.Count);
                    builder.AppendLiteral(":");

                    foreach (TypeSignature parameterType in pointer.Signature.ParameterTypes)
                    {
                        AppendPart(ref builder, GetTypeName(parameterType));
                    }

                    // Vararg parameters after the sentinel are distinct from the fixed parameter list
                    builder.AppendFormatted(pointer.Signature.IncludeSentinel);
                    builder.AppendFormatted(pointer.Signature.SentinelParameterTypes.Count);
                    builder.AppendLiteral(":");

                    foreach (TypeSignature parameterType in pointer.Signature.SentinelParameterTypes)
                    {
                        AppendPart(ref builder, GetTypeName(parameterType));
                    }

                    break;

                // Ordinary named types are identified by their resolved definition and assembly scope
                default:
                    AppendDescriptor(ref builder, type);
                    break;
            }

            // Share one string across equivalent signatures from different modules without allocating for a hit
            if (_uniqueTypeNames.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(builder.Text, out string? existingName))
            {
                name = existingName;

                builder.Clear();
            }
            else
            {
                name = builder.ToStringAndClear();

                _ = _uniqueTypeNames.Add(name);
            }

            _typeNames.Add(type, name);
        }

        return name;
    }

    /// <summary>
    /// Appends a fully scoped identity, distinguishing nested types from namespace-qualified names.
    /// </summary>
    /// <param name="builder">The target handler.</param>
    /// <param name="type">The type to format.</param>
    private void AppendDescriptor(ref DefaultInterpolatedStringHandler builder, ITypeDescriptor type)
    {
        // Discovery compares forwarded references by their resolved identity. Fingerprint the
        // same identity rather than whichever forwarding scope won a parallel dictionary insert.
        if (type.TryResolve(_state.RuntimeContext, out TypeDefinition? definition))
        {
            type = definition;
        }

        AppendPart(ref builder, type.Namespace);
        AppendPart(ref builder, type.Name);

        if (type.DeclaringType is { } declaringType)
        {
            builder.AppendLiteral("+");
            AppendDescriptor(ref builder, declaringType);
        }
        else
        {
            builder.AppendLiteral("@");
            AppendPart(ref builder, GetAssemblyName(type.Scope?.GetAssembly()));
        }
    }

    /// <summary>
    /// Length-prefixes names so unusual metadata identifiers cannot collide with separators.
    /// </summary>
    /// <param name="builder">The target handler.</param>
    /// <param name="value">The value to append, if available.</param>
    private static void AppendPart(ref DefaultInterpolatedStringHandler builder, string? value)
    {
        builder.AppendFormatted(value?.Length ?? -1);
        builder.AppendLiteral(":");
        builder.AppendFormatted(value);
    }

    /// <summary>
    /// Gets the canonical name of a resolved type definition.
    /// </summary>
    /// <param name="type">The type definition to format.</param>
    /// <returns>The unambiguous, assembly-qualified name of <paramref name="type"/>.</returns>
    private string GetDefinitionName(TypeDefinition type)
    {
        if (!_definitionNames.TryGetValue(type, out string? name))
        {
            name = GetTypeName(type.ToTypeSignature());
            _definitionNames.Add(type, name);
        }

        return name;
    }

    /// <summary>
    /// Formats an assembly identity only once per metadata object.
    /// </summary>
    /// <param name="assembly">The assembly to format, if available.</param>
    /// <returns>The full name of <paramref name="assembly"/>, or an empty string.</returns>
    private string GetAssemblyName(AssemblyDescriptor? assembly)
    {
        if (assembly is null)
        {
            return "";
        }

        if (!_assemblyNames.TryGetValue(assembly, out string? name))
        {
            name = assembly.FullName;
            _assemblyNames.Add(assembly, name);
        }

        return name;
    }
}
