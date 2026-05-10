// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using static WindowsRuntime.ProjectionWriter.References.ProjectionNames;

using static WindowsRuntime.ProjectionWriter.References.WellKnownNamespaces;

using static WindowsRuntime.ProjectionWriter.References.WellKnownTypeNames;

namespace WindowsRuntime.ProjectionWriter.Helpers;

internal static partial class AbiTypeHelpers
{
    /// <summary>
    /// Returns the (possibly mapped) namespace of a type signature, or 'System' for fundamentals.
    /// </summary>
    internal static string GetMappedNamespace(TypeSignature sig)
    {
        // Fundamentals (string, bool, int, etc.) live in 'System' for ArrayMarshaller path purposes.
        if (sig is CorLibTypeSignature) { return "System"; }
        ITypeDefOrRef? td = null;
        if (sig is TypeDefOrRefSignature tds) { td = tds.Type; }
        else if (sig is GenericInstanceTypeSignature gi) { td = gi.GenericType; }
        if (td is null) { return string.Empty; }
        (string typeNs, string typeName) = td.Names();
        MappedType? mapped = MappedTypes.Get(typeNs, typeName);
        return mapped is not null ? mapped.MappedNamespace : typeNs;
    }

    /// <summary>
    /// True if the type is a mapped value type that requires marshalling between projected and ABI
    /// representations (e.g. Windows.Foundation.DateTime &lt;-&gt; System.DateTimeOffset,
    /// Windows.Foundation.TimeSpan &lt;-&gt; System.TimeSpan, Windows.Foundation.HResult &lt;-&gt; System.Exception).
    /// These types use 'global::ABI.&lt;MappedNamespace&gt;.&lt;MappedName&gt;' as their ABI representation
    /// and need an explicit marshaller call ('global::ABI.&lt;MappedNamespace&gt;.&lt;MappedName&gt;Marshaller.ConvertToUnmanaged'/
    /// 'ConvertToManaged') to convert values across the boundary.
    /// </summary>
    private static bool IsMappedMarshalingValueType(TypeSignature sig, out string mappedNs, out string mappedName)
    {
        mappedNs = string.Empty;
        mappedName = string.Empty;
        ITypeDefOrRef? td = null;
        if (sig is TypeDefOrRefSignature tds) { td = tds.Type; }
        if (td is null) { return false; }
        (string ns, string name) = td.Names();
        // The set of mapped types that use the 'value-type marshaller' pattern (DateTime, TimeSpan, HResult).
        // Uri is also a mapped marshalling type but it's a reference type (handled via UriMarshaller separately).
        if (ns == WindowsFoundation)
        {
            if (name == "DateTime") { mappedNs = "System"; mappedName = "DateTimeOffset"; return true; }
            if (name == "TimeSpan") { mappedNs = "System"; mappedName = "TimeSpan"; return true; }
            if (name == HResult) { mappedNs = "System"; mappedName = "Exception"; return true; }
        }
        return false;
    }

    /// <summary>
    /// True if the type is a mapped value type that needs ABI marshalling (excluding HResult, handled separately).
    /// </summary>
    internal static bool IsMappedAbiValueType(TypeSignature sig)
    {
        if (!IsMappedMarshalingValueType(sig, out _, out string mappedName)) { return false; }
        // HResult/Exception is treated specially in many places; this helper is for DateTime/TimeSpan only.
        return mappedName != "Exception";
    }

    /// <summary>
    /// Returns the ABI type name for a mapped value type (e.g. 'global::ABI.System.TimeSpan').
    /// </summary>
    internal static string GetMappedAbiTypeName(TypeSignature sig)
    {
        if (!IsMappedMarshalingValueType(sig, out string ns, out string name)) { return string.Empty; }
        return GlobalAbiPrefix + ns + "." + name;
    }

    /// <summary>
    /// Returns the marshaller class name for a mapped value type (e.g. 'global::ABI.System.TimeSpanMarshaller').
    /// </summary>
    internal static string GetMappedMarshallerName(TypeSignature sig)
    {
        if (!IsMappedMarshalingValueType(sig, out string ns, out string name)) { return string.Empty; }
        return GlobalAbiPrefix + ns + "." + name + MarshallerSuffix;
    }
}
