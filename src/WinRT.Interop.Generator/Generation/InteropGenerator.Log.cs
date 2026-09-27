// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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

    private static void WriteLog(InteropGeneratorArgs args, InteropGeneratorDiscoveryState state, ModuleDefinition module)
    {
        string directory = args.LogDirectory!;

        if (!Directory.Exists(directory))
        {
            throw WellKnownInteropExceptions.LogDirectoryDoesNotExist(directory);
        }

        string path = Path.Combine(directory, LogFileName);
        string temporaryPath = Path.Combine(directory, $"interop-log.{Guid.NewGuid():N}.tmp");

        try
        {
            using (FileStream stream = File.Create(temporaryPath))
            using (Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true }))
            {
                WriteLogContent(writer, args, state, module);
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

    private static void WriteLogContent(Utf8JsonWriter writer, InteropGeneratorArgs args, InteropGeneratorDiscoveryState state, ModuleDefinition module)
    {
        RuntimeContext context = state.RuntimeContext;

        writer.WriteStartObject();
        writer.WriteNumber("schema", 1);
        writer.WriteNumber("assemblySizeBytes", new FileInfo(Path.Combine(args.GeneratedAssemblyDirectory, InteropNames.WindowsRuntimeInteropDllName)).Length);

        writer.WriteStartArray("typeHierarchy");

        foreach ((string type, string baseType) in state.TypeHierarchyEntries.OrderBy(static entry => entry.Key, StringComparer.Ordinal))
        {
            args.Token.ThrowIfCancellationRequested();
            writer.WriteStartObject();
            writer.WriteString("type", type);
            writer.WriteString("baseType", baseType);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        writer.WriteStartObject("genericInstantiations");
        WriteTypeArray(writer, "genericDelegates", state.GenericDelegateTypes, context, args.Token);
        WriteTypeArray(writer, "enumerators", state.IEnumerator1Types, context, args.Token);
        WriteTypeArray(writer, "enumerables", state.IEnumerable1Types, context, args.Token);
        WriteTypeArray(writer, "lists", state.IList1Types, context, args.Token);
        WriteTypeArray(writer, "readOnlyLists", state.IReadOnlyList1Types, context, args.Token);
        WriteTypeArray(writer, "dictionaries", state.IDictionary2Types, context, args.Token);
        WriteTypeArray(writer, "readOnlyDictionaries", state.IReadOnlyDictionary2Types, context, args.Token);
        WriteTypeArray(writer, "observableVectors", state.IObservableVector1Types, context, args.Token);
        WriteTypeArray(writer, "observableMaps", state.IObservableMap2Types, context, args.Token);
        WriteTypeArray(writer, "mapChangedEventArgs", state.IMapChangedEventArgs1Types, context, args.Token);
        WriteTypeArray(writer, "asyncActionsWithProgress", state.IAsyncActionWithProgress1Types, context, args.Token);
        WriteTypeArray(writer, "asyncOperations", state.IAsyncOperation1Types, context, args.Token);
        WriteTypeArray(writer, "asyncOperationsWithProgress", state.IAsyncOperationWithProgress2Types, context, args.Token);
        WriteTypeArray(writer, "keyValuePairs", state.KeyValuePairTypes, context, args.Token);
        writer.WriteEndObject();

        WriteInterfaceTypes(writer, "userDefinedTypes", state.UserDefinedAndVtableTypes, context, args.Token);
        WriteInterfaceTypes(writer, "arrayTypes", state.SzArrayAndVtableTypes, context, args.Token);

        writer.WriteStartArray("generatedTypes");

        foreach (TypeDefinition type in module.GetAllTypes().OrderBy(static type => type.FullName, StringComparer.Ordinal))
        {
            args.Token.ThrowIfCancellationRequested();
            writer.WriteStartObject();
            writer.WriteString("name", type.FullName);
            writer.WriteNumber("methodCount", type.Methods.Count);
            writer.WriteNumber("fieldCount", type.Fields.Count);
            writer.WriteNumber("ilInstructionCount", type.Methods.Sum(static method => method.CilMethodBody?.Instructions.Count ?? 0));
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteTypeArray(
        Utf8JsonWriter writer,
        string name,
        IEnumerable<GenericInstanceTypeSignature> types,
        RuntimeContext context,
        System.Threading.CancellationToken token)
    {
        writer.WriteStartArray(name);

        foreach (string type in types.Select(type => GetTypeName(type, context)).Order(StringComparer.Ordinal))
        {
            token.ThrowIfCancellationRequested();
            writer.WriteStringValue(type);
        }

        writer.WriteEndArray();
    }

    private static void WriteInterfaceTypes<T>(
        Utf8JsonWriter writer,
        string name,
        IReadOnlyDictionary<T, TypeSignatureEquatableSet> types,
        RuntimeContext context,
        System.Threading.CancellationToken token)
        where T : TypeSignature
    {
        writer.WriteStartArray(name);

        foreach ((string type, TypeSignatureEquatableSet interfaces) in types
            .Select(pair => (Type: GetTypeName(pair.Key, context), Interfaces: pair.Value))
            .OrderBy(static pair => pair.Type, StringComparer.Ordinal))
        {
            token.ThrowIfCancellationRequested();
            writer.WriteStartObject();
            writer.WriteString("type", type);
            writer.WriteStartArray("interfaces");

            foreach (string interfaceName in interfaces.Select(iface => GetTypeName(iface, context)).Order(StringComparer.Ordinal))
            {
                writer.WriteStringValue(interfaceName);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static string GetTypeName(ITypeDescriptor type, RuntimeContext context)
    {
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
