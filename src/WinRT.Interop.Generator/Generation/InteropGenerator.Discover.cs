// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.Resolvers;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <inheritdoc cref="InteropGenerator"/>
internal partial class InteropGenerator
{
    /// <summary>
    /// Runs the discovery logic for the generator.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <returns>The resulting state.</returns>
    private static InteropGeneratorState Discover(InteropGeneratorArgs args)
    {
        PathAssemblyResolver pathAssemblyResolver = new(args.ReferencePath);

        InteropGeneratorState state = new() { AssemblyResolver = pathAssemblyResolver };

        foreach (string path in args.ReferencePath.Concat([args.AssemblyPath]))
        {
            try
            {
                ModuleDefinition module = ModuleDefinition.FromFile(path, pathAssemblyResolver.ReaderParameters);

                state.TrackModuleDefinition(path, module);

                if (!module.AssemblyReferences.Any(static reference => reference.Name?.AsSpan().SequenceEqual("Microsoft.Windows.SDK.NET.dll"u8) is true) &&
                    module.Name?.AsSpan().SequenceEqual("Microsoft.Windows.SDK.NET.dll"u8) is not true &&
                    module.Name?.AsSpan().SequenceEqual("Microsoft.Windows.UI.Xaml.dll"u8) is not true)
                {
                    Console.WriteLine($"SKIPPED {Path.GetFileNameWithoutExtension(path)}");

                    continue;
                }

                Console.WriteLine($"Loaded {Path.GetFileNameWithoutExtension(path)}");

                foreach (TypeDefinition type in module.GetAllTypes())
                {
                    if (type.IsClass &&
                        !type.IsValueType &&
                        !type.IsDelegate &&
                        !(type.IsAbstract && type.IsSealed) &&
                        type.BaseType is not null &&
                        !SignatureComparer.Default.Equals(type.BaseType, module.CorLibTypeFactory.Object) &&
                        type.HasCustomAttribute("WinRT", "WindowsRuntimeTypeAttribute") &&
                        type.BaseType.HasCustomAttribute("WinRT", "WindowsRuntimeTypeAttribute"))
                    {
                        state.TrackTypeHierarchyEntry(type.FullName, type.BaseType.FullName);
                    }
                }

                foreach (TypeSpecification typeSpecification in module.EnumerateTableMembers<TypeSpecification>(TableIndex.TypeSpec))
                {
                    if (typeSpecification.Resolve() is { IsDelegate: true } &&
                        typeSpecification.Signature is GenericInstanceTypeSignature { GenericType.Name.Value: "TypedEventHandler`2" } typeSignature)
                    {
                        state.TrackGenericDelegateType(typeSignature);
                    }

                    if (typeSpecification.Resolve() is { IsValueType: true } &&
                        typeSpecification.Signature is GenericInstanceTypeSignature { GenericType.Name.Value: "KeyValuePair`2" } keyValuePairType)
                    {
                        state.TrackKeyValuePairType(keyValuePairType);
                    }
                }
            }
            catch (BadImageFormatException e)
            {
                Console.WriteLine($"FAILED {Path.GetFileNameWithoutExtension(path)}");
            }
        }

        return state;
    }
}
