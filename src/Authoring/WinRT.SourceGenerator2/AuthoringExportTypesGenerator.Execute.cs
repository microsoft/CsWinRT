// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using WindowsRuntime.SourceGenerator.Models;

namespace WindowsRuntime.SourceGenerator;

/// <inheritdoc cref="AuthoringExportTypesGenerator"/>
public partial class AuthoringExportTypesGenerator
{
    /// <summary>
    /// Generation methods for <see cref="AuthoringExportTypesGenerator"/>.
    /// </summary>
    private static class Execute
    {
        /// <summary>
        /// Gets the options for the generator.
        /// </summary>
        /// <param name="provider">The input options provider.</param>
        /// <param name="token">The cancellation token for the operation.</param>
        /// <returns>The resulting options.</returns>
        public static AuthoringExportTypesOptions GetOptions(AnalyzerConfigOptionsProvider provider, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            return new(
                PublishAot: provider.GlobalOptions.GetPublishAot(),
                IsComponent: provider.GlobalOptions.GetCsWinRTComponent(),
                MergeReferencedActivationFactories: provider.GlobalOptions.GetCsWinRTMergeReferencedActivationFactories());
        }

        /// <summary>
        /// Gets the info for generating native exports.
        /// </summary>
        /// <param name="data">The input data.</param>
        /// <param name="token">The cancellation token for the operation.</param>
        /// <returns>The resulting info.</returns>
        public static AuthoringNativeExportsInfo GetNativeExportsInfo((Compilation compilation, AuthoringExportTypesOptions Options) data, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            return new(
                AssemblyName: data.compilation.AssemblyName ?? "",
                Options: data.Options);
        }

        /// <summary>
        /// Gets all portable executable references from a compilation.
        /// </summary>
        /// <param name="data">The input data.</param>
        /// <param name="token">The cancellation token for the operation.</param>
        /// <returns>All portable executable references for the input compilation.</returns>
        public static AuthoringManagedExportsInfo GetManagedExportsInfo((Compilation Compilation, AuthoringExportTypesOptions Options) data, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            // Skip going through references if merging is not enabled
            if (!data.Options.MergeReferencedActivationFactories)
            {
                return new(
                    AssemblyName: data.Compilation.AssemblyName ?? "",
                    MergedManagedExportsTypeNames: [],
                    Options: data.Options);
            }

            ImmutableArray<string>.Builder builder = ImmutableArray.CreateBuilder<string>();

            // Go through all references to find transitively-references Windows Runtime components
            foreach (MetadataReference metadataReference in data.Compilation.References)
            {
                token.ThrowIfCancellationRequested();

                if (data.Compilation.GetAssemblyOrModuleSymbol(metadataReference) is not IAssemblySymbol assemblySymbol)
                {
                    continue;
                }

                token.ThrowIfCancellationRequested();

                // Add the type name if the assembly is a Windows Runtime component
                if (Helpers.TryGetDependentAssemblyExportsTypeName(
                    assemblySymbol: assemblySymbol,
                    compilation: data.Compilation,
                    token: token,
                    name: out string? name))
                {
                    builder.Add(name);
                }
            }

            token.ThrowIfCancellationRequested();

            return new(
                AssemblyName: data.Compilation.AssemblyName ?? "",
                MergedManagedExportsTypeNames: builder.ToImmutable(),
                Options: data.Options);
        }

        /// <summary>
        /// Emits the managed exports for authored components.
        /// </summary>
        /// <param name="context">The <see cref="SourceProductionContext"/> instance to use.</param>
        /// <param name="info">The input info.</param>
        public static void EmitManagedExports(SourceProductionContext context, AuthoringManagedExportsInfo info)
        {
            if (!info.Options.ShouldEmitManagedExports())
            {
                return;
            }

            IndentedTextWriter writer = new(literalLength: 0, formattedCount: 0);

            // Emit the '[WindowsRuntimeAuthoringAssemblyExportsType]' attribute so other tooling (including this same generator)
            // can reliably find the generated export types from other assemblies, which is needed when merging activation factories.
            writer.WriteLine($$"""
                // <auto-generated/>
                #pragma warning disable

                [assembly: global::WindowsRuntime.InteropServices.WindowsRuntimeAuthoringAssemblyExportsType(typeof(global::ABI.{{info.AssemblyName.EscapeIdentifierName()}}.ManagedExports))]
                
                namespace ABI.{{info.AssemblyName.EscapeIdentifierName()}};

                using global::System;
                using global::System.CodeDom.Compiler;
                using global::System.ComponentModel;
                using global::System.Diagnostics;
                using global::System.Diagnostics.CodeAnalysis;
                using global::System.Runtime.CompilerServices;

                /// <summary>
                /// Contains the managed exports for activating types from the current project in an authoring scenario.
                /// </summary>
                """);

            // Emit the standard generated attributes, and also mark the type as hidden, since it's generated as a public type
            writer.WriteGeneratedAttributes(nameof(AuthoringExportTypesGenerator), useFullyQualifiedTypeNames: false);
            writer.WriteLine($$"""
                [EditorBrowsable(EditorBrowsableState.Never)]
                public static unsafe class ManagedExports
                """);

            // Indent via a block, as we'll also need to emit custom logic for the activation factory
            using (writer.WriteBlock())
            {
                // The 'GetActivationFactory' method is the one that actually contains all the high-level logic for
                // activation scenarios. If we have any native exports (see below), those would call this method too.
                writer.WriteLine($$"""
                    /// <summary>
                    /// Retrieves the activation factory from a DLL that contains activatable Windows Runtime classes.
                    /// </summary>
                    /// <param name="activatableClassId">The class identifier that is associated with an activatable runtime class.</param>
                    /// <returns>The resulting pointer to the activation factory that corresponds with the class specified by <paramref name="activatableClassId"/>.</returns>
                    public static void* GetActivationFactory(ReadOnlySpan<char> activatableClassId)
                    """);

                using (writer.WriteBlock())
                {
                    // Emit the unsafe accessor pointing to 'WinRT.Authoring.dll', where the actual activation code is located
                    if (info.Options.IsComponent)
                    {
                        writer.WriteLine($"""
                            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, MethodName = "GetActivationFactory")]
                            static extern void* AuthoringGetActivationFactory(
                                [UnsafeAccessorType("ABI.{info.AssemblyName.EscapeIdentifierName()}.ManagedExports, WinRT.Authoring")] object? _,
                                ReadOnlySpan<char> activatableClassId);
                            """);
                    }

                    // Emit the specialized code to redirect the activation
                    if (info.Options.IsComponent && !info.Options.MergeReferencedActivationFactories)
                    {
                        writer.WriteLine("return AuthoringGetActivationFactory(null, activatableClassId);");
                    }
                    else if (info.Options.IsComponent && info.Options.MergeReferencedActivationFactories)
                    {
                        writer.WriteLine("""
                            void* activationFactory = AuthoringGetActivationFactory(null, activatableClassId);

                            return activationFactory ?? ReferencedManagedExports.GetActivationFactory(activatableClassId);
                            """);
                    }
                    else if (info.Options.MergeReferencedActivationFactories)
                    {
                        writer.WriteLine("ReferencedManagedExports.GetActivationFactory(activatableClassId);");
                    }
                }

                // Emit the reflection friendly overload as well
                writer.WriteLine();
                writer.WriteLine($$"""
                    /// <inheritdoc cref="GetActivationFactory(ReadOnlySpan{char})"/>
                    /// <remarks>This overload uses reflection friendly types and is only meant to be used from managed runtime hosts.</remarks>
                    public static nint GetActivationFactory(string activatableClassId)
                    {
                        return (nint)GetActivationFactory(activatableClassId.AsSpan());
                    }
                    """);
            }

            // Emit a helper type with the logic for merging activaton factories, if needed
            if (info.Options.MergeReferencedActivationFactories)
            {
                writer.WriteLine();
                writer.WriteLine("""
                    /// <summary>
                    /// Contains the logic for activating types from transitively referenced Windows Runtime components.
                    /// </summary>
                    """);
                writer.WriteGeneratedAttributes(nameof(AuthoringExportTypesGenerator), useFullyQualifiedTypeNames: false);
                writer.WriteLine("file static unsafe class ReferencedManagedExports");

                using (writer.WriteBlock())
                {
                    writer.WriteLine("""
                        /// <summary>
                        /// Retrieves the activation factory from all dependent Windows Runtime components.
                        /// </summary>
                        /// <param name="activatableClassId">The class identifier that is associated with an activatable runtime class.</param>
                        /// <returns>The resulting pointer to the activation factory that corresponds with the class specified by <paramref name="activatableClassId"/>.</returns>
                        public static void* GetActivationFactory(ReadOnlySpan<char> activatableClassId)
                        """);

                    using (writer.WriteBlock())
                    {
                        writer.WriteLine("void* activationFactory");

                        // Iterate through all transitvely referenced activation factories from other components and use the first that succeeds
                        foreach (string managedExportsTypeName in info.MergedManagedExportsTypeNames)
                        {
                            writer.WriteLine();
                            writer.WriteLine($$"""
                                activationFactory = global::{{managedExportsTypeName}}.GetActivationFactory(activatableClassId);

                                if (activationFactory is not null)
                                {
                                    return activationFactory;
                                }
                                """);
                        }

                        // No match across the referenced factories, we can't do anything else
                        writer.WriteLine();
                        writer.WriteLine("return null;");
                    }
                }
            }

            context.AddSource("ManagedExports.g.cs", writer.ToStringAndClear());
        }

        /// <summary>
        /// Emits the native exports for authored components.
        /// </summary>
        /// <param name="context">The <see cref="SourceProductionContext"/> instance to use.</param>
        /// <param name="info">The input info.</param>
        public static void EmitNativeExports(SourceProductionContext context, AuthoringNativeExportsInfo info)
        {
            if (!info.Options.ShouldEmitNativeExports())
            {
                return;
            }

            IndentedTextWriter writer = new(literalLength: 0, formattedCount: 0);

            // Because we're generating code in our own namespace (which nobody else will use), we can use namespace-scoped
            // using directives, which allows the generated code to be more concise. We can also do the same with type
            // aliases, so we can emit the native methods with a signature that more closely matches the original.
            writer.WriteLine($"""
                // <auto-generated/>
                #pragma warning disable
                
                namespace ABI.{info.AssemblyName.EscapeIdentifierName()};

                using global::System;
                using global::System.CodeDom.Compiler;
                using global::System.Diagnostics;
                using global::System.Diagnostics.CodeAnalysis;
                using global::System.Runtime.CompilerServices;
                using global::System.Runtime.InteropServices;
                using global::WindowsRuntime.InteropServices;

                using HRESULT = int;
                using unsafe HSTRING = void*;
                using IActivationFactory = void;

                /// <summary>
                /// Contains the native exports for activating types from the current project in an authoring scenario.
                /// </summary>
                """);

            // Emit the attributes to mark the code as generated, and to exclude it from code coverage as well. We also use a
            // file-scoped P/Invoke, so we don't take a dependency on private implementation detail types from 'WinRT.Runtime.dll'.
            writer.WriteGeneratedAttributes(nameof(AuthoringExportTypesGenerator), useFullyQualifiedTypeNames: false);
            writer.WriteLine($$"""
                internal static unsafe class NativeExports
                {
                    /// <summary>
                    /// Retrieves the activation factory from a DLL that contains activatable Windows Runtime classes.
                    /// </summary>
                    /// <param name="activatableClassId">The class identifier that is associated with an activatable runtime class.</param>
                    /// <param name="factory">A pointer to the activation factory that corresponds with the class specified by <paramref name="activatableClassId"/>.</param>
                    /// <returns>The <c>HRESULT</c> for the operation.</returns>
                    /// <seealso href="https://learn.microsoft.com/en-us/previous-versions/br205771(v=vs.85)"/>
                    [UnmanagedCallersOnly(EntryPoint = nameof(DllGetActivationFactory), CallConvs = [typeof(CallConvStdcall)])]
                    public static HRESULT DllGetActivationFactory(HSTRING activatableClassId, IActivationFactory** factory)
                    {
                        const int E_INVALIDARG = unchecked((int)0x80070057);
                        const int CLASS_E_CLASSNOTAVAILABLE = unchecked((int)(0x80040111));
                        const int S_OK = 0;
                
                        if (activatableClassId is null || factory is null)
                        {
                            return E_INVALIDARG;
                        }

                        ReadOnlySpan<char> managedActivatableClassId = HStringMarshaller.ConvertToManagedUnsafe(activatableClassId);

                        try
                        {
                            IActivationFactory* result = ManagedExports.GetActivationFactory(managedActivatableClassId);

                            if ((void*)obj is null)
                            {
                                *factory = null;
                
                                return CLASS_E_CLASSNOTAVAILABLE;
                            }
                
                            *factory = (void*)obj;
                
                            return S_OK;
                        }
                        catch (Exception e)
                        {
                            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
                        }

                        {GenerateNativeDllGetActivationFactoryImpl(context)}
                    }
                
                    /// <summary>
                    /// Determines whether the DLL that implements this function is in use. If not, the caller can unload the DLL from memory.
                    /// </summary>
                    /// <returns>This method always returns <c>S_FALSE</c>.</returns>
                    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/api/combaseapi/nf-combaseapi-dllcanunloadnow"/>
                    [UnmanagedCallersOnly(EntryPoint = nameof(DllCanUnloadNow), CallConvs = [typeof(CallConvStdcall)])]
                    public static HRESULT DllCanUnloadNow()
                    {
                        const HRESULT S_FALSE = 1;
                
                        return S_FALSE;
                    }
                }

                /// <summary>
                /// A marshaller for the Windows Runtime <c>HSTRING</c> type.
                /// </summary>
                file static class HStringMarshaller
                {
                    /// <summary>
                    /// Converts an input <c>HSTRING</c> value to a <see cref="ReadOnlySpan{T}"/> value.
                    /// </summary>
                    /// <param name="value">The input <c>HSTRING</c> value to marshal.</param>
                    /// <returns>The resulting <see cref="ReadOnlySpan{T}"/> value.</returns>
                    public static ReadOnlySpan<char> ConvertToManagedUnsafe(HSTRING value)
                    {
                        uint length;
                        char* buffer = WindowsRuntimeImports.WindowsGetStringRawBuffer(value, &length);

                        return new(buffer, (int)length);
                    }
                }

                /// <summary>
                /// Native imports for fundamental COM and Windows Runtime APIs.
                /// </summary>
                file static class WindowsRuntimeImports
                {
                    /// <see href="https://learn.microsoft.com/windows/win32/api/winstring/nf-winstring-windowsgetstringrawbuffer"/>
                    [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", EntryPoint = "WindowsGetStringRawBuffer", ExactSpelling = true)]
                    [SupportedOSPlatform("windows6.2")]
                    public static extern partial char* WindowsGetStringRawBuffer(HSTRING @string, uint* length);
                }
                """);

            context.AddSource("NativeExports.g.cs", writer.ToStringAndClear());
        }
    }
}