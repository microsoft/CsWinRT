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
using ConsoleAppFramework;
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
    private InteropGenerationFingerprint(InteropGeneratorArgs args, InteropGeneratorDiscoveryState state)
    {
        _args = args;
        _state = state;
        _inputModules = new(state.Modules.Values, ReferenceEqualityComparer.Instance);

        foreach (ModuleDefinition module in state.Modules.Values)
        {
            if (module.IsWindowsRuntimeModule ||
                module.IsBaseClassLibraryModule ||
                module.Assembly is { IsWindowsRuntimeReferenceAssembly: true })
            {
                _ = _implementationModules.Add(module);
            }
        }

        foreach (ModuleDefinition? module in new[]
        {
            state.WindowsRuntimeSdkProjectionModule,
            state.WindowsRuntimeSdkXamlProjectionModule,
            state.WindowsRuntimeProjectionModule,
            state.WindowsRuntimeComponentModule
        })
        {
            if (module is not null)
            {
                _ = _implementationModules.Add(module);
            }
        }
    }

    /// <summary>
    /// Computes a cache key, or reports why this invocation cannot be cached.
    /// </summary>
    /// <param name="args">The invocation arguments.</param>
    /// <param name="state">The completed discovery state.</param>
    /// <returns>The SHA256 fingerprint, or <see langword="null"/> if caching is unavailable.</returns>
    public static byte[]? TryCreate(InteropGeneratorArgs args, InteropGeneratorDiscoveryState state)
    {
        try
        {
            return new InteropGenerationFingerprint(args, state).Create();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or NotSupportedException or BadImageFormatException)
        {
            ConsoleApp.Log($"Incremental interop generation unavailable: {e.Message}");

            return null;
        }
        catch (AggregateException e) when (e.InnerExceptions.All(static error => error is IOException or UnauthorizedAccessException or NotSupportedException or BadImageFormatException))
        {
            ConsoleApp.Log($"Incremental interop generation unavailable: {e.Message}");

            return null;
        }
    }

    /// <summary>
    /// Hashes the discovery roots and their metadata dependencies in a deterministic order.
    /// </summary>
    private byte[] Create()
    {
        using SHA256 hash = SHA256.Create();

        using (CryptoStream crypto = new(Stream.Null, hash, CryptoStreamMode.Write))
        using (BufferedStream buffer = new(crypto, 16384))
        using (BinaryWriter writer = new(buffer, Encoding.UTF8))
        {
            writer.Write("CsWinRT.Interop.EmissionInputs.1");
            WriteGeneratorIdentity(writer);
            writer.Write(_state.RuntimeContext.TargetRuntime.ToString());
            writer.Write(_state.Modules[_args.OutputAssemblyPath].Assembly?.Version.ToString() ?? "");
            writer.Write(_args.UseWindowsUIXamlProjections);
            writer.Write((int)_args.MarshallingMode);
            writer.Write(_args.GenerateCollectionChangedListVtables);
            writer.Write(_args.ValidateWinRTRuntimeAssemblyVersion);
            writer.Write(_args.ValidateWinRTRuntimeDllVersion2References);
            writer.Write(_args.TreatWarningsAsErrors);

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

            writer.Write(_state.TypeHierarchyEntries.Count);

            foreach ((string name, string baseName) in _state.TypeHierarchyEntries.OrderBy(static entry => entry.Key, StringComparer.Ordinal))
            {
                writer.Write(name);
                writer.Write(baseName);
            }

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

            writer.Write(_state.UserDefinedVtableTypes.Count);

            string[][] vtableSets = [.. _state.UserDefinedVtableTypes.Select(types =>
            {
                foreach (TypeSignature type in types)
                {
                    _pendingTypes.Enqueue(type);
                }

                return types.Select(GetTypeName).Order(StringComparer.Ordinal).ToArray();
            })];

            // Do not use the emit set comparer here: it repeatedly formats and sorts signatures
            // inside each comparison. Materialize each name once for this additional pass.
            Array.Sort(vtableSets, static (left, right) =>
            {
                for (int i = 0; i < Math.Min(left.Length, right.Length); i++)
                {
                    int result = StringComparer.Ordinal.Compare(left[i], right[i]);

                    if (result != 0)
                    {
                        return result;
                    }
                }

                return left.Length.CompareTo(right.Length);
            });

            foreach (string[] types in vtableSets)
            {
                writer.Write(types.Length);

                foreach (string type in types)
                {
                    writer.Write(type);
                }
            }

            DiscoverMetadataDependencies();
            writer.Write(_definitions.Count);

            foreach (TypeDefinition definition in _definitions
                .OrderBy(GetDefinitionName, StringComparer.Ordinal)
                .ThenBy(static type => type.DeclaringModule?.FilePath, StringComparer.Ordinal))
            {
                _args.Token.ThrowIfCancellationRequested();
                WriteTypeDefinition(writer, definition);
            }

            ModuleDefinition[] implementationModules = [.. _implementationModules
                .OrderBy(static module => $"{module.Name}[{module.Assembly}]", StringComparer.Ordinal)
                .ThenBy(static module => module.FilePath, StringComparer.Ordinal)];
            byte[][] implementationHashes = new byte[implementationModules.Length][];

            _ = Parallel.For(
                fromInclusive: 0,
                toExclusive: implementationModules.Length,
                parallelOptions: new ParallelOptions { CancellationToken = _args.Token, MaxDegreeOfParallelism = _args.MaxDegreesOfParallelism },
                body: i =>
                {
                    using FileStream stream = File.OpenRead(implementationModules[i].FilePath ?? throw new NotSupportedException("An emission dependency has no backing file."));

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

        return hash.Hash!;
    }

    /// <summary>
    /// Includes the actual generator build, not just its public assembly version.
    /// </summary>
    private static void WriteGeneratorIdentity(BinaryWriter writer)
    {
        writer.Write(Environment.Version.ToString());

        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            using FileStream stream = File.OpenRead(Environment.ProcessPath ?? throw new NotSupportedException("The native generator path is unavailable."));

            writer.Write(SHA256.HashData(stream));
        }
        else
        {
            // Managed development runs also depend on the separately deployed generator libraries.
            string[] paths = [.. Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll").Order(StringComparer.Ordinal)];

            writer.Write(paths.Length);

            foreach (string path in paths)
            {
                writer.Write(Path.GetFileName(path));

                using FileStream stream = File.OpenRead(path);

                writer.Write(SHA256.HashData(stream));
            }
        }
    }

    /// <summary>
    /// Writes a module's binding identity and the name used by IgnoresAccessChecksTo.
    /// </summary>
    private void WriteModuleIdentity(BinaryWriter writer, ModuleDefinition module)
    {
        writer.Write(module.Name?.Value ?? "");
        writer.Write(GetAssemblyName(module.Assembly));
        writer.Write((uint)(module.Assembly?.Attributes ?? 0));
    }

    /// <summary>
    /// Writes an unordered set of discovered types with explicit collection boundaries.
    /// </summary>
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
        HashSet<TypeSignature> visited = new(ReferenceEqualityComparer.Instance);

        while (_pendingTypes.TryDequeue(out TypeSignature? signature))
        {
            _args.Token.ThrowIfCancellationRequested();

            if (!visited.Add(signature))
            {
                continue;
            }

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

            if (signature is GenericParameterSignature or FunctionPointerTypeSignature or SentinelTypeSignature ||
                !signature.TryResolve(_state.RuntimeContext, out TypeDefinition? definition) ||
                !_definitions.Add(definition))
            {
                continue;
            }

            ModuleDefinition declaringModule = definition.DeclaringModule!;

            if (!_implementationModules.Contains(declaringModule) &&
                (definition.IsProjectedWindowsRuntimeType || !_inputModules.Contains(declaringModule)))
            {
                _ = _implementationModules.Add(declaringModule);
            }

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
    private void WriteTypeDefinition(BinaryWriter writer, TypeDefinition type)
    {
        writer.Write(GetDefinitionName(type));
        WriteModuleIdentity(writer, type.DeclaringModule!);
        writer.Write(_implementationModules.Contains(type.DeclaringModule!));

        if (_implementationModules.Contains(type.DeclaringModule!))
        {
            return;
        }

        writer.Write((uint)type.Attributes);
        writer.Write(type.BaseType is { } baseType ? GetTypeName(baseType.ToTypeSignature(_state.RuntimeContext)) : "");
        writer.Write(type.ClassLayout?.ClassSize ?? 0);
        writer.Write(type.ClassLayout?.PackingSize ?? 0);
        writer.Write(type.Interfaces.Count);

        foreach (InterfaceImplementation implementation in type.Interfaces)
        {
            writer.Write(implementation.Interface is { } interfaceType ? GetTypeName(interfaceType.ToTypeSignature(_state.RuntimeContext)) : "");
        }

        writer.Write(type.GenericParameters.Count);

        foreach (GenericParameter parameter in type.GenericParameters)
        {
            writer.Write((int)parameter.Attributes);
        }

        WriteCustomAttributes(writer, type);
        writer.Write(type.IsValueType);

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
    private string GetTypeName(TypeSignature type)
    {
        if (!_typeNames.TryGetValue(type, out string? name))
        {
            DefaultInterpolatedStringHandler builder = new(0, 0, CultureInfo.InvariantCulture);

            builder.AppendFormatted((int)type.ElementType);
            builder.AppendLiteral(":");

            switch (type)
            {
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
                case GenericParameterSignature parameter:
                    builder.AppendFormatted((int)parameter.ParameterType);
                    builder.AppendLiteral(":");
                    builder.AppendFormatted(parameter.Index);
                    break;
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

                    builder.AppendFormatted(pointer.Signature.IncludeSentinel);
                    builder.AppendFormatted(pointer.Signature.SentinelParameterTypes.Count);
                    builder.AppendLiteral(":");

                    foreach (TypeSignature parameterType in pointer.Signature.SentinelParameterTypes)
                    {
                        AppendPart(ref builder, GetTypeName(parameterType));
                    }

                    break;
                default:
                    AppendDescriptor(ref builder, type);
                    break;
            }

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
    private void AppendDescriptor(ref DefaultInterpolatedStringHandler builder, ITypeDescriptor type)
    {
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
    private static void AppendPart(ref DefaultInterpolatedStringHandler builder, string? value)
    {
        builder.AppendFormatted(value?.Length ?? -1);
        builder.AppendLiteral(":");
        builder.AppendFormatted(value);
    }

    /// <summary>
    /// Gets the canonical name of a resolved type definition.
    /// </summary>
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
