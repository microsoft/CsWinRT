// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Helpers that need access to the metadata cache (and so cannot be modeled as
/// pure extension methods on AsmResolver types).
/// </summary>
internal static class Helpers
{
    /// <summary>
    /// Returns the type referenced by an <c>[ExclusiveTo]</c> attribute on the given interface,
    /// or <see langword="null"/> if the interface is not exclusive-to anything (or the attribute
    /// argument cannot be resolved). Mirrors the C++ logic that walks an interface's
    /// <c>Windows.Foundation.Metadata.ExclusiveToAttribute</c> and reads its <c>System.Type</c> argument.
    /// </summary>
    /// <param name="iface">The interface type definition to inspect.</param>
    /// <param name="cache">The metadata cache used to resolve the referenced type.</param>
    /// <returns>The exclusive-to type, or <see langword="null"/>.</returns>
    public static TypeDefinition? GetExclusiveToType(TypeDefinition iface, MetadataCache cache)
    {
        for (int i = 0; i < iface.CustomAttributes.Count; i++)
        {
            CustomAttribute attr = iface.CustomAttributes[i];
            ITypeDefOrRef? attrType = attr.Constructor?.DeclaringType;
            if (attrType is null) { continue; }
            string ns = attrType.Namespace?.Value ?? string.Empty;
            string name = attrType.Name?.Value ?? string.Empty;
            if (ns != "Windows.Foundation.Metadata" || name != "ExclusiveToAttribute") { continue; }
            if (attr.Signature is null) { continue; }
            for (int j = 0; j < attr.Signature.FixedArguments.Count; j++)
            {
                CustomAttributeArgument arg = attr.Signature.FixedArguments[j];
                if (arg.Element is TypeSignature sig)
                {
                    string typeName = sig.FullName ?? string.Empty;
                    TypeDefinition? td = cache.Find(typeName);
                    if (td is not null) { return td; }
                }
                else if (arg.Element is AsmResolver.Utf8String s)
                {
                    TypeDefinition? td = cache.Find(s.Value);
                    if (td is not null) { return td; }
                }
                else if (arg.Element is string ss)
                {
                    TypeDefinition? td = cache.Find(ss);
                    if (td is not null) { return td; }
                }
            }
        }
        return null;
    }
}
