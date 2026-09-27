// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.Generator.Errors;
using WindowsRuntime.InteropGenerator.Errors;
using WindowsRuntime.InteropGenerator.Models;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <inheritdoc cref="InteropGenerator"/>
internal partial class InteropGenerator
{
    private const string LogFileName = "interop-log.json";

    /// <summary>
    /// Writes the opt-in report after the interop assembly has been emitted, if requested.
    /// </summary>
    /// <param name="args">The generator arguments, including the optional log directory.</param>
    /// <param name="state">The completed discovery state.</param>
    /// <param name="module">The generated interop module.</param>
    private static void WriteLog(InteropGeneratorArgs args, InteropGeneratorDiscoveryState state, ModuleDefinition module)
    {
        if (args.LogDirectory is not { } directory)
        {
            return;
        }

        if (!Directory.Exists(directory))
        {
            throw WellKnownInteropExceptions.LogDirectoryDoesNotExist(directory);
        }

        string path = Path.Combine(directory, LogFileName);
        string temporaryPath = Path.Combine(directory, $"interop-log.{Guid.NewGuid():N}.tmp");

        // Publish only a complete report so a failed write cannot leave an apparently valid incremental output
        try
        {
            InteropGeneratorLog log = CreateLog(args, state, module);

            using (FileStream stream = File.Create(temporaryPath))
            {
                JsonSerializer.Serialize(stream, log, InteropGeneratorLogJsonSerializerContext.Default.InteropGeneratorLog);
            }

            File.Move(temporaryPath, path, overwrite: true);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw WellKnownInteropExceptions.WriteLogError(path, e);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    /// <summary>
    /// Collects the versioned discovery and emitted-code data for serialization.
    /// </summary>
    /// <param name="args">The generator arguments.</param>
    /// <param name="state">The completed discovery state.</param>
    /// <param name="module">The generated interop module.</param>
    /// <returns>The complete interop log document.</returns>
    private static InteropGeneratorLog CreateLog(InteropGeneratorArgs args, InteropGeneratorDiscoveryState state, ModuleDefinition module)
    {
        RuntimeContext context = state.RuntimeContext;

        // Read the emitted file rather than estimating its size from in-memory metadata
        long assemblySizeBytes = new FileInfo(Path.Combine(args.GeneratedAssemblyDirectory, InteropNames.WindowsRuntimeInteropDllName)).Length;

        // Include shared helpers and nested types that do not appear in any discovery category
        InteropGeneratorLog.GeneratedType[] generatedTypes = [.. module.GetAllTypes()
            .OrderBy(static type => type.FullName, StringComparer.Ordinal)
            .Select(type =>
            {
                args.Token.ThrowIfCancellationRequested();

                return new InteropGeneratorLog.GeneratedType(
                    type.FullName,
                    type.Methods.Count,
                    type.Fields.Count,
                    type.Methods.Sum(static method => method.CilMethodBody?.Instructions.Count ?? 0));
            })];

        // Sort hierarchy entries because parallel discovery does not guarantee enumeration order
        InteropGeneratorLog.TypeHierarchyEntry[] typeHierarchy = [.. state.TypeHierarchyEntries
            .OrderBy(static entry => entry.Key, StringComparer.Ordinal)
            .Select(entry =>
            {
                args.Token.ThrowIfCancellationRequested();

                return new InteropGeneratorLog.TypeHierarchyEntry(entry.Key, entry.Value);
            })];

        // These final discovery sets include instantiations added transitively by other types
        InteropGeneratorLog.GenericInstantiationsInfo genericInstantiations = new(
            GenericDelegates: GetTypeArray(state.GenericDelegateTypes, context, args.Token),
            Enumerators: GetTypeArray(state.IEnumerator1Types, context, args.Token),
            Enumerables: GetTypeArray(state.IEnumerable1Types, context, args.Token),
            Lists: GetTypeArray(state.IList1Types, context, args.Token),
            ReadOnlyLists: GetTypeArray(state.IReadOnlyList1Types, context, args.Token),
            Dictionaries: GetTypeArray(state.IDictionary2Types, context, args.Token),
            ReadOnlyDictionaries: GetTypeArray(state.IReadOnlyDictionary2Types, context, args.Token),
            ObservableVectors: GetTypeArray(state.IObservableVector1Types, context, args.Token),
            ObservableMaps: GetTypeArray(state.IObservableMap2Types, context, args.Token),
            MapChangedEventArgs: GetTypeArray(state.IMapChangedEventArgs1Types, context, args.Token),
            AsyncActionsWithProgress: GetTypeArray(state.IAsyncActionWithProgress1Types, context, args.Token),
            AsyncOperations: GetTypeArray(state.IAsyncOperation1Types, context, args.Token),
            AsyncOperationsWithProgress: GetTypeArray(state.IAsyncOperationWithProgress2Types, context, args.Token),
            KeyValuePairs: GetTypeArray(state.KeyValuePairTypes, context, args.Token));

        return new(
            Schema: 1,
            AssemblySizeBytes: assemblySizeBytes,
            TypeHierarchy: typeHierarchy,
            GenericInstantiations: genericInstantiations,
            UserDefinedTypes: GetInterfaceTypes(state.UserDefinedAndVtableTypes, context, args.Token),
            ArrayTypes: GetInterfaceTypes(state.SzArrayAndVtableTypes, context, args.Token),
            GeneratedTypes: generatedTypes);
    }

    /// <summary>
    /// Gets a sorted discovery category of constructed generic types.
    /// </summary>
    /// <param name="types">The discovered types in the category.</param>
    /// <param name="context">The context for resolving type identities.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>The assembly-qualified type names.</returns>
    private static string[] GetTypeArray(
        IEnumerable<GenericInstanceTypeSignature> types,
        RuntimeContext context,
        CancellationToken token)
    {
        // Concurrent discovery has no stable enumeration order; normalize names before sorting
        return [.. types.Select(type =>
        {
            token.ThrowIfCancellationRequested();

            return GetTypeName(type, context);
        }).Order(StringComparer.Ordinal)];
    }

    /// <summary>
    /// Gets discovered types alongside the interfaces used for their COM entries.
    /// </summary>
    /// <typeparam name="T">The type signature used as the discovery key.</typeparam>
    /// <param name="types">The discovered types and their interface sets.</param>
    /// <param name="context">The context for resolving type identities.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>The types and their sorted interface names.</returns>
    private static InteropGeneratorLog.TypeWithInterfaces[] GetInterfaceTypes<T>(
        IReadOnlyDictionary<T, TypeSignatureEquatableSet> types,
        RuntimeContext context,
        CancellationToken token)
        where T : TypeSignature
    {
        // Keep each type's interface list even when multiple types share emitted COM entry helpers
        return [.. types.Select(pair =>
        {
            token.ThrowIfCancellationRequested();

            return new InteropGeneratorLog.TypeWithInterfaces(
                GetTypeName(pair.Key, context),
                [.. pair.Value.Select(iface => GetTypeName(iface, context)).Order(StringComparer.Ordinal)]);
        }).OrderBy(static entry => entry.Type, StringComparer.Ordinal)];
    }

    /// <summary>
    /// Formats a type with its resolved assembly identity and any generic or array arguments.
    /// </summary>
    /// <param name="type">The type to format.</param>
    /// <param name="context">The context for resolving forwarded type references.</param>
    /// <returns>A stable, assembly-qualified type name where resolution is available.</returns>
    private static string GetTypeName(ITypeDescriptor type, RuntimeContext context)
    {
        // Resolving the outer type alone would lose constructed arguments and array shape
        if (type is GenericInstanceTypeSignature generic)
        {
            return $"{GetTypeName(generic.GenericType, context)}<{string.Join(", ", generic.TypeArguments.Select(argument => GetTypeName(argument, context)))}>";
        }

        if (type is SzArrayTypeSignature array)
        {
            return $"{GetTypeName(array.BaseType, context)}[]";
        }

        ITypeDescriptor resolved = type.TryResolve(context, out TypeDefinition? definition) ? definition : type;
        string? assemblyName = resolved.Scope?.GetAssembly()?.Name?.ToString();

        return assemblyName is null ? resolved.FullName : $"{assemblyName}:{resolved.FullName}";
    }
}
