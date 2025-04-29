#pragma warning disable

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.InteropServices;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class TypeMapAttribute<TTypeMapGroup> : Attribute
{
    public TypeMapAttribute(string value, Type target) { }

    [RequiresUnreferencedCode("Interop types may be removed by trimming")]
    public TypeMapAttribute(string value, Type target, Type trimTarget) { }
}

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class TypeMapAssemblyTargetAttribute<TTypeMapGroup> : Attribute
{
    public TypeMapAssemblyTargetAttribute(string assemblyName) { }
}

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class TypeMapAssociationAttribute<TTypeMapGroup> : Attribute
{
    public TypeMapAssociationAttribute(Type source, Type proxy) { }
}

public static class TypeMapping
{
    [RequiresUnreferencedCode("Interop types may be removed by trimming")]
    public static IReadOnlyDictionary<string, Type> GetOrCreateExternalTypeMapping<TTypeMapGroup>()
    {
        return null!;
    }

    [RequiresUnreferencedCode("Interop types may be removed by trimming")]
    public static IReadOnlyDictionary<Type, Type> GetOrCreateProxyTypeMapping<TTypeMapGroup>()
    {
        return null!;
    }
}