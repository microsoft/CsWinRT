// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using AsmResolver.DotNet;
using WindowsRuntime.InteropGenerator.Factories;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Builders;

/// <summary>
/// A builder for the metadata assembly attributes.
/// </summary>
internal static partial class MetadataAssemblyAttributesBuilder
{
    /// <summary>
    /// The set of well known attribute types to copy over from the runtime assembly.
    /// </summary>
    private static readonly FrozenSet<string> WellKnownAttributeTypes =
    [
        typeof(CompilationRelaxationsAttribute).FullName!,
        typeof(RuntimeCompatibilityAttribute).FullName!,
        typeof(TargetFrameworkAttribute).FullName!,
        typeof(SupportedOSPlatformAttribute).FullName!,
#pragma warning disable SYSLIB0003 // Type or member is obsolete
        typeof(SecurityPermissionAttribute).FullName!,
#pragma warning restore SYSLIB0003
        typeof(UnverifiableCodeAttribute).FullName!
    ];

    /// <summary>
    /// Defines all assembly attributes for the interop module.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    public static void AssemblyAttributes(InteropReferences interopReferences, ModuleDefinition module)
    {
        ModuleDefinition windowsRuntimeModule = (ModuleDefinition)interopReferences.WindowsRuntimeModule;

        // Copy over all assembly attributes from the runtime assembly
        foreach (CustomAttribute assemblyAttribute in windowsRuntimeModule.Assembly!.CustomAttributes)
        {
            if (!WellKnownAttributeTypes.Contains(assemblyAttribute.Constructor?.DeclaringType?.FullName ?? ""))
            {
                continue;
            }

            module.Assembly!.CustomAttributes.Add(new CustomAttribute(
                constructor: (ICustomAttributeType)assemblyAttribute.Constructor!.ImportWith(module.DefaultImporter),
                signature: assemblyAttribute.Signature));
        }

        // Add the '[AssemblyMetadata]' attributes to mark the assembly as trimmable and AOT compatible.
        // Technically speaking marking it as trimmable isn't required to let trimming work when using
        // Native AOT, however it's good practice and does matter when trimming while using CoreCLR.
        module.Assembly!.CustomAttributes.Add(InteropCustomAttributeFactory.AssemblyMetadata("IsTrimmable", "True", interopReferences, module));
        module.Assembly!.CustomAttributes.Add(InteropCustomAttributeFactory.AssemblyMetadata("IsAotCompatible", "True", interopReferences, module));

        // Add the '[DisableRuntimeMarshalling]' attribute
        module.Assembly!.CustomAttributes.Add(InteropCustomAttributeFactory.DisableRuntimeMarshalling(interopReferences));

        // Copy over all module attributes from the runtime assembly
        foreach (CustomAttribute moduleAttribute in windowsRuntimeModule.CustomAttributes)
        {
            if (!WellKnownAttributeTypes.Contains(moduleAttribute.Constructor?.DeclaringType?.FullName ?? ""))
            {
                continue;
            }

            module.CustomAttributes.Add(new CustomAttribute(
                constructor: (ICustomAttributeType)moduleAttribute.Constructor!.ImportWith(module.DefaultImporter),
                signature: moduleAttribute.Signature));
        }
    }
}
