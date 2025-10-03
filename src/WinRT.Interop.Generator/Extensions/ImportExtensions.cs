// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions to import metadata elements into modules.
/// </summary>
internal static class ImportExtensions
{
    /// <summary>
    /// Imports a type definition or reference into a module using the default reference importer.
    /// </summary>
    /// <param name="typeDefOrRef">The <see cref="ITypeDefOrRef"/> instance to import.</param>
    /// <param name="module">The module to import into.</param>
    /// <returns>The imported <see cref="ITypeDefOrRef"/>.</returns>
    public static ITypeDefOrRef Import(this ITypeDefOrRef typeDefOrRef, ModuleDefinition module)
    {
        return typeDefOrRef.ImportWith(module.DefaultImporter);
    }

    /// <summary>
    /// Imports a method definition or reference into a module using the default reference importer.
    /// </summary>
    /// <param name="methodDefOrRef">The <see cref="IMethodDefOrRef"/> instance to import.</param>
    /// <param name="module">The module to import into.</param>
    /// <returns>The imported <see cref="IMethodDefOrRef"/>.</returns>
    public static IMethodDefOrRef Import(this IMethodDefOrRef methodDefOrRef, ModuleDefinition module)
    {
        return (IMethodDefOrRef)methodDefOrRef.ImportWith(module.DefaultImporter);
    }

    /// <summary>
    /// Imports a type signature into a module using the default reference importer.
    /// </summary>
    /// <param name="typeSignature">The <see cref="TypeSignature"/> instance to import.</param>
    /// <param name="module">The module to import into.</param>
    /// <returns>The imported <see cref="TypeSignature"/>.</returns>
    public static TypeSignature Import(this TypeSignature typeSignature, ModuleDefinition module)
    {
        return typeSignature.ImportWith(module.DefaultImporter);
    }

    /// <summary>
    /// Imports a type signature into a module using the default reference importer.
    /// </summary>
    /// <param name="typeSignature">The <see cref="TypeSignature"/> instance to import.</param>
    /// <param name="module">The module to import into.</param>
    /// <returns>The imported <see cref="TypeSignature"/>.</returns>
    public static GenericInstanceTypeSignature Import(this GenericInstanceTypeSignature typeSignature, ModuleDefinition module)
    {
        return (GenericInstanceTypeSignature)typeSignature.ImportWith(module.DefaultImporter);
    }

    /// <summary>
    /// Imports a method signature into a module using the default reference importer.
    /// </summary>
    /// <param name="methodSignature">The <see cref="MethodSignature"/> instance to import.</param>
    /// <param name="module">The module to import into.</param>
    /// <returns>The imported <see cref="MethodSignature"/>.</returns>
    public static MethodSignature Import(this MethodSignature methodSignature, ModuleDefinition module)
    {
        return methodSignature.ImportWith(module.DefaultImporter);
    }

    /// <summary>
    /// Imports a member reference into a module using the default reference importer.
    /// </summary>
    /// <param name="assemblyReference">The <see cref="AssemblyReference"/> instance to import.</param>
    /// <param name="module">The module to import into.</param>
    /// <returns>The imported <see cref="AssemblyReference"/>.</returns>
    public static AssemblyReference Import(this AssemblyReference assemblyReference, ModuleDefinition module)
    {
        return assemblyReference.ImportWith(module.DefaultImporter);
    }

    /// <summary>
    /// Imports a member reference into a module using the default reference importer.
    /// </summary>
    /// <param name="memberReference">The <see cref="MemberReference"/> instance to import.</param>
    /// <param name="module">The module to import into.</param>
    /// <returns>The imported <see cref="MemberReference"/>.</returns>
    public static MemberReference Import(this MemberReference memberReference, ModuleDefinition module)
    {
        return memberReference.ImportWith(module.DefaultImporter);
    }

    /// <summary>
    /// Imports a method specification into a module using the default reference importer.
    /// </summary>
    /// <param name="methodSpecification">The <see cref="MethodSpecification"/> instance to import.</param>
    /// <param name="module">The module to import into.</param>
    /// <returns>The imported <see cref="MethodSpecification"/>.</returns>
    public static MethodSpecification Import(this MethodSpecification methodSpecification, ModuleDefinition module)
    {
        return methodSpecification.ImportWith(module.DefaultImporter);
    }
}
