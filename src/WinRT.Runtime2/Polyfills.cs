#pragma warning disable

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.InteropServices;

/// <summary>
/// Base interface for target type universe.
/// </summary>
public interface ITypeMapUniverse { }

/// <summary>
/// Type mapping between a string and a type.
/// </summary>
/// <typeparam name="TTypeUniverse">Type universe</typeparam>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class TypeMapAttribute<TTypeUniverse> : Attribute
    where TTypeUniverse : ITypeMapUniverse
{
    /// <summary>
    /// Create a mapping between a value and a <see cref="System.Type"/>.
    /// </summary>
    /// <param name="value">String representation of key</param>
    /// <param name="target">Type value</param>
    /// <remarks>
    /// This mapping is unconditionally inserted into the type map.
    /// </remarks>
    public TypeMapAttribute(string value, Type target)
    { }

    /// <summary>
    /// Create a mapping between a value and a <see cref="System.Type"/>.
    /// </summary>
    /// <param name="value">String representation of key</param>
    /// <param name="target">Type value</param>
    /// <param name="trimTarget">Type used by Trimmer to determine type map inclusion.</param>
    /// <remarks>
    /// This mapping is only included in the type map if the Trimmer observes a type check
    /// using the <see cref="System.Type"/> represented by <paramref name="trimTarget"/>.
    /// </remarks>
    //[RequiresUnreferencedCode("Interop types may be removed by trimming")]
    public TypeMapAttribute(string value, Type target, Type trimTarget)
    { }
}

/// <summary>
/// Declare an assembly that should be inspected during type map building.
/// </summary>
/// <typeparam name="TTypeUniverse">Type universe</typeparam>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class TypeMapAssemblyTargetAttribute<TTypeUniverse> : Attribute
    where TTypeUniverse : ITypeMapUniverse
{
    /// <summary>
    /// Provide the assembly to look for type mapping attributes.
    /// </summary>
    /// <param name="assemblyName">Assembly to reference</param>
    public TypeMapAssemblyTargetAttribute(string assemblyName)
    { }
}

/// <summary>
/// Create a type association between a type and its proxy.
/// </summary>
/// <typeparam name="TTypeUniverse">Type universe</typeparam>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class TypeMapAssociationAttribute<TTypeUniverse> : Attribute
    where TTypeUniverse : ITypeMapUniverse
{
    /// <summary>
    /// Create an association between two types in the type map.
    /// </summary>
    /// <param name="source">Target type.</param>
    /// <param name="proxy">Type to associated with <paramref name="source"/>.</param>
    /// <remarks>
    /// This mapping will only exist in the type map if the Trimmer observes
    /// an allocation using the <see cref="System.Type"/> represented by <paramref name="source"/>.
    /// </remarks>
    public TypeMapAssociationAttribute(Type source, Type proxy)
    { }
}

/// <summary>
/// Entry type for interop type mapping logic.
/// </summary>
public static class TypeMapping
{
    /// <summary>
    /// Returns the External type type map generated for the current application.
    /// </summary>
    /// <typeparam name="TTypeUniverse">Type universe</typeparam>
    /// <param name="map">Requested type map</param>
    /// <returns>True if the map is returned, otherwise false.</returns>
    /// <remarks>
    /// Call sites are treated as an intrinsic by the Trimmer and implemented inline.
    /// </remarks>
    [RequiresUnreferencedCode("Interop types may be removed by trimming")]
    public static IReadOnlyDictionary<string, Type> GetExternalTypeMapping<TTypeUniverse>()
        where TTypeUniverse : ITypeMapUniverse
    {
        return null!;
    }

    /// <summary>
    /// Returns the associated type type map generated for the current application.
    /// </summary>
    /// <typeparam name="TTypeUniverse">Type universe</typeparam>
    /// <param name="map">Requested type map</param>
    /// <returns>True if the map is returned, otherwise false.</returns>
    /// <remarks>
    /// Call sites are treated as an intrinsic by the Trimmer and implemented inline.
    /// </remarks>
    [RequiresUnreferencedCode("Interop types may be removed by trimming")]
    public static IReadOnlyDictionary<Type, Type> GetTypeProxyMapping<TTypeUniverse>()
        where TTypeUniverse : ITypeMapUniverse
    {
        return null!;
    }
}