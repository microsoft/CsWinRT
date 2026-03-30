// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.UI.Xaml.Interop;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

#pragma warning disable IDE0008, IDE1006, CA1416

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
[assembly: TypeMap<WindowsRuntimeMetadataTypeMapGroup>(
    value: "Windows.UI.Xaml.Interop.TypeName",
    target: typeof(ABI.System.Type),
    trimTarget: typeof(Type))]

[assembly: TypeMap<WindowsRuntimeComWrappersTypeMapGroup>(
    value: "Windows.Foundation.IReference`1<Windows.UI.Xaml.Interop.TypeName>",
    target: typeof(ABI.System.Type),
    trimTarget: typeof(Type))]
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code

[assembly: TypeMapAssociation<WindowsRuntimeComWrappersTypeMapGroup>(typeof(Type), typeof(ABI.System.Type))]

namespace ABI.System;

/// <summary>
/// ABI type for <see cref="global::System.Type"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.typename"/>
[WindowsRuntimeMappedMetadata("Windows.Foundation.UniversalApiContract")]
[WindowsRuntimeClassName("Windows.Foundation.IReference`1<Windows.UI.Xaml.Interop.TypeName>")]
[WindowsRuntimeMetadataTypeName("Windows.UI.Xaml.Interop.TypeName")]
[WindowsRuntimeMappedType(typeof(global::System.Type))]
[TypeComWrappersMarshaller]
public unsafe struct Type
{
    /// <summary>
    /// The name of the type.
    /// </summary>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.typename.name"/>
    public HSTRING Name;

    /// <summary>
    /// A <see cref="TypeKind"/> value containing basic guidance regarding the origin of the type.
    /// </summary>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.typename.kind"/>
    public TypeKind Kind;
}

/// <summary>
/// Marshaller for <see cref="global::System.Type"/>.
/// </summary>
public static unsafe class TypeMarshaller
{
    /// <summary>
    /// Converts a managed <see cref="global::System.Type"/> to an unmanaged <see cref="Type"/>.
    /// </summary>
    /// <param name="value">The managed <see cref="global::System.Type"/> value.</param>
    /// <returns>The unmanaged <see cref="Type"/> value.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <see langword="null"/>.</exception>
    public static Type ConvertToUnmanaged(global::System.Type value)
    {
        ConvertToUnmanagedUnsafe(value, out TypeReference reference);

        return reference.ConvertToUnmanaged();
    }

    /// <summary>
    /// Converts a managed <see cref="global::System.Type"/> to an unmanaged <see cref="Type"/> with fast-pass.
    /// </summary>
    /// <param name="value">The managed <see cref="global::System.Type"/> value.</param>
    /// <param name="reference">
    /// The resulting <see cref="TypeReference"/> instance. This must be kept in scope as long
    /// as the <see cref="Type"/> value retrieved from it is being used. It is not valid to escape that
    /// value, as the reference is required to exist for the fast-pass <see cref="Type"/> to be valid.
    /// </param>
    /// <returns>The unmanaged <see cref="Type"/> value.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <see langword="null"/>.</exception>
    public static void ConvertToUnmanagedUnsafe(global::System.Type value, out TypeReference reference)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!WindowsRuntimeFeatureSwitches.EnableXamlTypeMarshalling)
        {
            throw TypeExceptions.GetNotSupportedExceptionForMarshallingDisabled();
        }

        ManagedTypeReference typeReference = TypeNameCache.TypeToTypeNameMap.GetOrAdd(value, UncachedTypeMarshaller.ToManagedTypeReference);

        reference = new TypeReference { Name = typeReference.Name, Kind = typeReference.Kind };
    }

    /// <summary>
    /// Converts an unmanaged <see cref="Type"/> to a managed <see cref="global::System.Type"/>.
    /// </summary>
    /// <param name="value">The unmanaged <see cref="Type"/> value.</param>
    /// <returns>The managed <see cref="global::System.Type"/> value</returns>
    public static global::System.Type? ConvertToManaged(Type value)
    {
        if (!WindowsRuntimeFeatureSwitches.EnableXamlTypeMarshalling)
        {
            throw TypeExceptions.GetNotSupportedExceptionForMarshallingDisabled();
        }

        ReadOnlySpan<char> typeName = HStringMarshaller.ConvertToManagedUnsafe(value.Name);

        // Get the alternate lookup from the cache first and use it to try to retrieve a cached 'Type'
        // instance. This allows us to avoid materializing the 'string' if we have a cache hit here.
        if (TypeNameCache.TypeNameToTypeMap.GetAlternateLookup<TransientTypeReference>().TryGetValue(
            key: new TransientTypeReference(typeName, value.Kind),
            value: out global::System.Type? result))
        {
            return result;
        }

        // We didn't get a cached value, so we just manually marshal the value here. Note that we
        // can't just use a 'GetOrAdd' overload here like above, because there isn't one available
        // that supports alternate lookups. But we don't want to give up on avoiding this allocation.
        result = UncachedTypeMarshaller.FromTransientTypeReference(new TransientTypeReference(typeName, value.Kind));

        // Try to add the value to the cache. If another thread already added it, we just ignore the result.
        // Marshalled 'Type' instances are guaranteed to have a 1:1 mapping, so we can still use the local
        // value that we just produced, instead of having to do another lookup to get it from the cache.
        _ = TypeNameCache.TypeNameToTypeMap.TryAdd(new ManagedTypeReference(typeName.ToString(), value.Kind), result);

        return result;
    }

    /// <inheritdoc cref="WindowsRuntimeValueTypeMarshaller.BoxToUnmanaged{T}(T?, CreateComInterfaceFlags, in Guid)"/>
    public static WindowsRuntimeObjectReferenceValue BoxToUnmanaged(global::System.Type? value)
    {
        return value is null ? default : new((void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.None, in WellKnownWindowsInterfaceIIDs.IID_IReferenceOfType));
    }

    /// <inheritdoc cref="WindowsRuntimeValueTypeMarshaller.UnboxToManaged(void*)"/>
    public static global::System.Type? UnboxToManaged(void* value)
    {
        Type? abi = WindowsRuntimeValueTypeMarshaller.UnboxToManaged<Type>(value);

        return abi.HasValue ? ConvertToManaged(abi.GetValueOrDefault()) : null;
    }

    /// <summary>
    /// Disposes resources associated with an unmanaged <see cref="Type"/> value.
    /// </summary>
    /// <param name="value">The unmanaged <see cref="Type"/> value to dispose.</param>
    public static void Dispose(Type value)
    {
        HStringMarshaller.Free(value.Name);
    }
}

/// <summary>
/// Represents a reference to a <see cref="Type"/> value, for fast marshalling to native.
/// </summary>
file readonly struct ManagedTypeReference
{
    /// <inheritdoc cref="Type.Name"/>
    public readonly string Name;

    /// <inheritdoc cref = "Type.Kind" />
    public readonly TypeKind Kind;

    /// <summary>
    /// Creates a new <see cref="ManagedTypeReference"/> value with the specified parameters.
    /// </summary>
    /// <param name="name"><inheritdoc cref="Type.Name" path="/summary/node()"/></param>
    /// <param name="kind"><inheritdoc cref = "Type.Kind" path="/summary/node()"/></param>
    public ManagedTypeReference(string name, TypeKind kind)
    {
        Name = name;
        Kind = kind;
    }
}

/// <summary>
/// Represents a transient reference to a <see cref="Type"/> value, to avoid allocations.
/// </summary>
file readonly ref struct TransientTypeReference
{
    /// <inheritdoc cref="Type.Name"/>
    public readonly ReadOnlySpan<char> Name;

    /// <inheritdoc cref = "Type.Kind" />
    public readonly TypeKind Kind;

    /// <summary>
    /// Creates a new <see cref="TransientTypeReference"/> value with the specified parameters.
    /// </summary>
    /// <param name="name"><inheritdoc cref="Type.Name" path="/summary/node()"/></param>
    /// <param name="kind"><inheritdoc cref = "Type.Kind" path="/summary/node()"/></param>
    public TransientTypeReference(ReadOnlySpan<char> name, TypeKind kind)
    {
        Name = name;
        Kind = kind;
    }
}

/// <summary>
/// A custom <see cref="IEqualityComparer{T}"/> for <see cref="ManagedTypeReference"/> to support zero-allocation lookups.
/// </summary>
file sealed class ManagedTypeReferenceEqualityComparer :
    IEqualityComparer<ManagedTypeReference>,
    IAlternateEqualityComparer<TransientTypeReference, ManagedTypeReference>
{
    /// <summary>
    /// The singleton <see cref="ManagedTypeReferenceEqualityComparer"/> instance.
    /// </summary>
    public static readonly ManagedTypeReferenceEqualityComparer Instance = new();

    /// <inheritdoc/>
    public ManagedTypeReference Create(TransientTypeReference alternate)
    {
        return new(alternate.Name.ToString(), alternate.Kind);
    }

    /// <inheritdoc/>
    public bool Equals(ManagedTypeReference x, ManagedTypeReference y)
    {
        return x.Kind == y.Kind && string.Equals(x.Name, y.Name, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public bool Equals(TransientTypeReference alternate, ManagedTypeReference other)
    {
        return alternate.Kind == other.Kind && alternate.Name.SequenceEqual(other.Name);
    }

    /// <inheritdoc/>
    public int GetHashCode(ManagedTypeReference obj)
    {
        return HashCode.Combine(
            value1: string.GetHashCode(obj.Name),
            value2: obj.Kind);
    }

    /// <inheritdoc/>
    public int GetHashCode(TransientTypeReference alternate)
    {
        return HashCode.Combine(
            value1: string.GetHashCode(alternate.Name),
            value2: alternate.Kind);
    }
}

/// <summary>
/// Cached maps to speedup <see cref="global::System.Type"/> marshalling.
/// </summary>
file static class TypeNameCache
{
    /// <summary>
    /// The cache of type names to <see cref="global::System.Type"/> instances.
    /// </summary>
    /// <remarks>
    /// This cache is mostly only used by XAML, meaning it should pretty much always be accessed from the UI thread.
    /// Because of this, we can set the concurrency level to just '1', to reduce the memory use from this dictionary.
    /// </remarks>
    public static readonly ConcurrentDictionary<ManagedTypeReference, global::System.Type?> TypeNameToTypeMap = new(
        concurrencyLevel: 1,
        capacity: 32,
        comparer: ManagedTypeReferenceEqualityComparer.Instance);

    /// <summary>
    /// The cache of <see cref="global::System.Type"/> instances to type name values.
    /// </summary>
    /// <remarks><inheritdoc cref="TypeNameToTypeMap" path="/remarks/node()"/></remarks>
    public static readonly ConcurrentDictionary<global::System.Type, ManagedTypeReference> TypeToTypeNameMap = new(concurrencyLevel: 1, capacity: 32);
}

/// <summary>
/// Marshaller for <see cref="global::System.Type"/> using no cache.
/// </summary>
file static class UncachedTypeMarshaller
{
    /// <summary>
    /// Converts a <see cref="global::System.Type"/> to a <see cref="ManagedTypeReference"/> value.
    /// </summary>
    /// <param name="value">The <see cref="global::System.Type"/> value.</param>
    /// <returns>The <see cref="ManagedTypeReference"/> value.</returns>
    public static ManagedTypeReference ToManagedTypeReference(global::System.Type value)
    {
        // Special case for 'NoMetadataTypeInfo' instances, which can only be obtained
        // from previous calls to 'ConvertToManaged' for types that had been trimmed.
        if (value is NoMetadataTypeInfo noMetadataTypeInfo)
        {
            return new(noMetadataTypeInfo.FullName, TypeKind.Metadata);
        }

        // We need special handling for 'Nullable<T>' values. If we have one, we want to use the underlying type
        // for the lookup, because the nullable version would not have any entries in the type map. Additionally,
        // we want to skip the metadata table lookup, as that would give us the actual metadata type name for the
        // underlying type. Instead, for 'Nullable<T>' values we need the runtime class name, which in this case
        // would be the 'IReference<T>' type name for boxed instances of this type.
        global::System.Type? nullableUnderlyingType = Nullable.GetUnderlyingType(value);

        // Special handling for some types which can never be used in a 'Nullable<T>' instantiation (e.g. interfaces)
        if (nullableUnderlyingType is null)
        {
            // For projected types (not custom-mapped, but possibly manually projected, like e.g. 'IAsyncInfo'), we
            // can always just use the fully qualified type name (as it will always match the one in the .winmd file).
            // We can check if a given type matches this by just checking whether it has '[WindowsRuntimeMetadata]'.
            // Note that we're intentionally skipping generic types, as for those we need the 'cswinrtinteropgen' info.
            // Additionally, this path isn't taken if we have a nullable value type, which avoids the lookup too.
            if (!value.IsGenericType && value.IsDefined(typeof(WindowsRuntimeMetadataAttribute)))
            {
                return new(value.FullName!, TypeKind.Metadata);
            }

            // Use the metadata info lookup first to handle custom-mapped interface types. These would not have a proxy
            // type map entry for normal marshalling (because they're interfaces), and they would also not show up as
            // being projected types from there. So we handle them here first to get the right metadata type name.
            if (WindowsRuntimeMetadataInfo.TryGetInfo(value, out WindowsRuntimeMetadataInfo? metadataInfo))
            {
                return new(metadataInfo.GetMetadataTypeName(), TypeKind.Metadata);
            }
        }

        // Special case 'Exception' types, since we also need to handle all derived types (e.g. user-defined)
        if (value.IsAssignableTo(typeof(global::System.Exception)))
        {
            return new("Windows.Foundation.HResult", TypeKind.Metadata);
        }

        // Special case 'Type' as well, for the same reason (e.g. 'typeof(Foo)' would return a 'RuntimeType' instance)
        if (value.IsAssignableTo(typeof(global::System.Type)))
        {
            return new("Windows.UI.Xaml.Interop.TypeName", TypeKind.Metadata);
        }

        global::System.Type typeOrUnderlyingType = nullableUnderlyingType ?? value;

        // Use the marshalling info lookup to detect projected or custom-mapped Windows Runtime types.
        // If we have an underlying nullable type, we use that for the lookup instead of the input type.
        if (WindowsRuntimeMarshallingInfo.TryGetInfo(typeOrUnderlyingType, out WindowsRuntimeMarshallingInfo? marshallingInfo) && marshallingInfo.IsMetadataType)
        {
            // For primitive types, we always report 'TypeKind.Primitive'. This means that some
            // types that are C# primitives (e.g. 'sbyte') will be reported as such, even though
            // they're not Windows Runtime types. This is expected, and matches C++/WinRT as well.
            TypeKind kind = value.IsPrimitive
                ? TypeKind.Primitive
                : TypeKind.Metadata;

            // We need special handling for several cases that represent non-boxed types
            if (nullableUnderlyingType is null)
            {
                // Special case primitive types that are of types that can be boxed. That is, if the input type is not
                // some 'Nullable<T>' type, check if we have an explicit metadata type name, and use that if so. This
                // will ensure that e.g. 'typeof(int)' will report 'Int32', not 'Windows.Foundation.IReference<Int32>'.
                // This will also handle generic delegate types, which will also use '[WindowsRuntimeMetadataTypeName]'.
                if (marshallingInfo.TryGetMetadataTypeName(out string? metadataTypeName))
                {
                    return new(metadataTypeName, kind);
                }

                // If the type is 'KeyValuePair<,>', we are guaranteed to have a runtime class name on the proxy type.
                // Note that because this type is a reference type in the Windows Runtime type system, the runtime class
                // name will be the correct name to use here, as it will just be the 'IKeyValuePair<,>' interface type.
                // This is why we don't need additional attributes for the metadata type name in this specific scenario.
                if (typeOrUnderlyingType.IsValueType &&
                    typeOrUnderlyingType.IsGenericType &&
                    typeOrUnderlyingType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    return new(marshallingInfo.GetRuntimeClassName(), kind);
                }

                // If we don't have a metadata type name, check if we have a value type or a delegate type.
                // Note that this path can only be reached for those if either of these is true:
                //   - The value type is not generic (the only possible case would've been 'KeyValuePair<,>')
                //   - The delegate is not generic (or the proxy type would've had a metadata name on it)
                // So in either case, we can just use the fully qualified type name here, which will match
                // the .winmd type name. We don't have to worry about custom-mapped types here, as those will
                // have already been handled above. This special case ensures we don't get the boxed type name.
                if (typeOrUnderlyingType.IsValueType ||
                    typeOrUnderlyingType.IsAssignableTo(typeof(Delegate)))
                {
                    return new(typeOrUnderlyingType.FullName!, kind);
                }
            }

            // We don't support marshalling a 'KeyValuePair<,>?' type, as that is not a valid Windows Runtime
            // type (since 'KeyValuePair<,>' is an interface in the Windows Runtime type system. So if we get
            // one, we just treat it like any other custom (e.g. user-defined) types instead.
            if (nullableUnderlyingType is not null &&
                nullableUnderlyingType.IsValueType &&
                nullableUnderlyingType.IsGenericType &&
                nullableUnderlyingType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                goto CustomType;
            }

            // If we don't have a metadata type name, try to get the runtime class name. This will handle
            // cases such as constructed 'Nullable<T>' types, which will report their boxed type name.
            if (marshallingInfo.TryGetRuntimeClassName(out string? runtimeClassName))
            {
                return new(runtimeClassName, kind);
            }

            // Otherwise, use the type name directly. This will handle all remaining cases, such as projected
            // runtime classes and interface types. For all of those, the projected type name will be correct.
            return new(typeOrUnderlyingType.FullName!, kind);
        }

        // For primitive types, we always report 'TypeKind.Primitive'. This means that some
        // types that are C# primitives (e.g. 'sbyte') will be reported as such, even though
        // they're not Windows Runtime types. This is expected, and matches C++/WinRT as well.
        // Note that all primitive types that are Windows Runtime types should have been handled
        // by the case above already. This path just ensures the other ones are not treated as
        // custom types, which they would be otherwise, since they don't have marshalling info.
        if (value.IsPrimitive)
        {
            return new(value.FullName!, TypeKind.Primitive);
        }

    CustomType:

        // All other cases are treated as custom types (e.g. user-defined types)
        return new(value.AssemblyQualifiedName!, TypeKind.Custom);
    }

    /// <summary>
    /// Converts a <see cref="TransientTypeReference"/> to a managed <see cref="global::System.Type"/>.
    /// </summary>
    /// <param name="value">The <see cref="TransientTypeReference"/> value.</param>
    /// <returns>The managed <see cref="global::System.Type"/> value</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "Any types which are trimmed are not used by managed user code and there is fallback logic to handle that.")]
    public static global::System.Type? FromTransientTypeReference(TransientTypeReference value)
    {
        ReadOnlySpan<char> typeName = value.Name;

        // Just return 'null' if we somehow received a default value
        if (typeName.IsEmpty)
        {
            return null;
        }

        // If the type is a custom type, we need to use the type name to get the managed type.
        // This API is not trim-safe, but there isn't really another way to implement this.
        // In theory, any types used in XAML should automatically be rooted by generated code.
        if (value.Kind is TypeKind.Custom)
        {
            return global::System.Type.GetType(typeName.ToString());
        }

        // Handle special-cases metadata types after checking the type kind
        if (value.Kind is TypeKind.Metadata)
        {
            // For any 'HResult' type, we return 'Exception'. If a user had marshalled
            // any derived exception type, that will not actually round-trip exactly.
            // This is expected and by design. We don't try to preserve the original.
            if (typeName.SequenceEqual("Windows.Foundation.HResult"))
            {
                return typeof(global::System.Exception);
            }

            // Same as above for 'Type'. We intentionally don't perfectly round-trip.
            if (typeName.SequenceEqual("Windows.UI.Xaml.Interop.TypeName"))
            {
                return typeof(global::System.Type);
            }

            // 'IBindableVectorView' has no equivalent .NET type, so we just return 'null' here. This needs
            // to be special-cased, or it would end up resolving a marshalling info instance (because the
            // external type map still has an entry for this name, for marshalling), and then fail because
            // there's no associated public type for the resulting proxy type retrieved from the type map.
            if (typeName.SequenceEqual(WellKnownXamlRuntimeClassNames.IBindableVectorView))
            {
                return null;
            }
        }

        global::System.Type? type = null;

        // If the type was handled by the metadata lookup, get the public type from there
        if (WindowsRuntimeMetadataInfo.TryGetInfo(typeName, out WindowsRuntimeMetadataInfo? metadataInfo))
        {
            type = metadataInfo.PublicType;
        }
        else if (WindowsRuntimeMarshallingInfo.TryGetInfo(typeName, out WindowsRuntimeMarshallingInfo? marshallingInfo))
        {
            // Otherwise, try to retrieve the marshalling info for the input type name.
            // This will work for both 'Primitive' and 'Metadata' types, same as above.
            global::System.Type publicType = marshallingInfo.PublicType;

            // If we got here, it means we have some 'IReference<T>' instance with the
            // element type being some delegate, exception, or 'Type' type. Because we
            // only support marshalling them as 'TypeName' by value (not references),
            // we're intentionally always just returning missing metadata for them.
            if (publicType.IsAssignableTo(typeof(Delegate)) ||
                publicType.IsAssignableTo(typeof(global::System.Exception)) ||
                publicType.IsAssignableTo(typeof(global::System.Type)))
            {
                return new NoMetadataTypeInfo(typeName.ToString());
            }

            if (publicType.IsValueType)
            {
                // Special case 'KeyValuePair<,>' instances, where we always want to return the public type
                // directly here, and not its nullable version. This is because 'KeyValuePair<,>' is an
                // interface type in the Windows Runtime type system, so the type name we got here from
                // the marshalling type map wouldn't actually represent an 'IReference<T>' instantiation.
                if (publicType.IsGenericType &&
                    publicType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    type = publicType;
                }
                else
                {
                    // For other value types, we get the reference type (i.e. the constructed 'Nullable<T>' type)
                    // from the marshalling info. This will perform a lookup for '[WindowsRuntimeReferenceType]'.
                    type = marshallingInfo.ReferenceType;
                }
            }
            else
            {
                // Otherwise, just use the public type directly
                type = publicType;
            }
        }

        // Handle the case of C# primitive types that are not Windows Runtime types.
        // For instance, 'System.SByte' could be passed, which would not be found
        // in the previous lookup. We still want to be able to round-trip such values.
        if (type is null && value.Kind is TypeKind.Primitive && typeName.StartsWith("System.", StringComparison.Ordinal))
        {
            return global::System.Type.GetType(typeName.ToString());
        }

        // If the target type is a projected type that has been trimmed, we can return a special type.
        // This is mostly used by the XAML metadata provider. The type itself should never actually be
        // used, as there's no C# references causing it to be rooted. If anyone tried to use it, the
        // returned implementation will just throw an exception for all unsupported operations on it.
        if (type is null && value.Kind is TypeKind.Metadata)
        {
            return new NoMetadataTypeInfo(typeName.ToString());
        }

        // Return whatever result we managed to get from the cache
        return type;
    }
}

/// <summary>
/// Exception stubs for marshalling <see cref="global::System.Type"/> objects.
/// </summary>
internal static unsafe class TypeExceptions
{
    /// <summary>
    /// Throws an <see cref="ArgumentException"/> when resolving a type fails.
    /// </summary>
    /// <param name="type">The type that failed to resolve.</param>
    [DoesNotReturn]
    [StackTraceHidden]
    public static void ThrowArgumentExceptionForNullType(Type type)
    {
        throw new ArgumentException(
            $"The type with name '{HStringMarshaller.ConvertToManaged(type.Name)}' and kind '{type.Kind}' cannot cannot be marshalled to a managed 'Type' instance. " +
            $"If the application is running with trimming enabled (or on Native AOT), it's possible the issue is caused by trimming causing all metadata for the type " +
            $"to be removed. To work around the issue, consider using the '[DynamicDependency]' attribute over the method causing this exception to eventually be thrown. " +
            $"You can see the API docs for this attribute here: https://learn.microsoft.com/dotnet/api/system.diagnostics.codeanalysis.dynamicdependencyattribute.");
    }

    /// <summary>
    /// Gets a <see cref="NotSupportedException"/> if marshalling support is disabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static NotSupportedException GetNotSupportedExceptionForMarshallingDisabled()
    {
        // We can't throw the exception from here like the other method above, because it causes
        // a linker crash when publishing with Native AOT. We can update this once it's fixed.
        return new(
            $"Support for marshalling 'System.Type' values is disabled (make sure that the 'CsWinRTEnableXamlTypeMarshalling' property is not set to 'false'). " +
            $"In this configuration, marshalling a 'System.Type' value directly to native code or to managed will always fail. Additionally, marshalling a " +
            $"boxed 'System.Type' object as an untyped parameter for a Windows Runtime API will result in the CCW using the same layout as for 'object'.");
    }
}

/// <summary>
/// The set of <see cref="ComInterfaceEntry"/> values for <see cref="global::System.Type"/>.
/// </summary>
file struct TypeInterfaceEntries
{
    public ComInterfaceEntry IReferenceOfType;
    public ComInterfaceEntry IPropertyValue;
    public ComInterfaceEntry IStringable;
    public ComInterfaceEntry IWeakReferenceSource;
    public ComInterfaceEntry IMarshal;
    public ComInterfaceEntry IAgileObject;
    public ComInterfaceEntry IInspectable;
    public ComInterfaceEntry IUnknown;
}

/// <summary>
/// The implementation of <see cref="TypeInterfaceEntries"/>.
/// </summary>
file static class TypeInterfaceEntriesImpl
{
    /// <summary>
    /// The <see cref="TypeInterfaceEntries"/> value for <see cref="global::System.Type"/>.
    /// </summary>
    [FixedAddressValueType]
    public static readonly TypeInterfaceEntries Entries;

    /// <summary>
    /// Initializes <see cref="Entries"/>.
    /// </summary>
    static TypeInterfaceEntriesImpl()
    {
        Entries.IReferenceOfType.IID = WellKnownWindowsInterfaceIIDs.IID_IReferenceOfType;
        Entries.IReferenceOfType.Vtable = TypeReferenceImpl.Vtable;
        Entries.IPropertyValue.IID = WellKnownWindowsInterfaceIIDs.IID_IPropertyValue;
        Entries.IPropertyValue.Vtable = IPropertyValueImpl.OtherTypeVtable;
        Entries.IStringable.IID = WellKnownWindowsInterfaceIIDs.IID_IStringable;
        Entries.IStringable.Vtable = IStringableImpl.Vtable;
        Entries.IWeakReferenceSource.IID = WellKnownWindowsInterfaceIIDs.IID_IWeakReferenceSource;
        Entries.IWeakReferenceSource.Vtable = IWeakReferenceSourceImpl.Vtable;
        Entries.IMarshal.IID = WellKnownWindowsInterfaceIIDs.IID_IMarshal;
        Entries.IMarshal.Vtable = IMarshalImpl.Vtable;
        Entries.IAgileObject.IID = WellKnownWindowsInterfaceIIDs.IID_IAgileObject;
        Entries.IAgileObject.Vtable = IAgileObjectImpl.Vtable;
        Entries.IInspectable.IID = WellKnownWindowsInterfaceIIDs.IID_IInspectable;
        Entries.IInspectable.Vtable = IInspectableImpl.Vtable;
        Entries.IUnknown.IID = WellKnownWindowsInterfaceIIDs.IID_IUnknown;
        Entries.IUnknown.Vtable = IUnknownImpl.Vtable;
    }
}

/// <summary>
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="global::System.Type"/>.
/// </summary>
file sealed unsafe class TypeComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override void* GetOrCreateComInterfaceForObject(object value)
    {
        return WindowsRuntimeComWrappersMarshal.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.TrackerSupport);
    }

    /// <inheritdoc/>
    public override ComInterfaceEntry* ComputeVtables(out int count)
    {
        count = sizeof(TypeInterfaceEntries) / sizeof(ComInterfaceEntry);

        return (ComInterfaceEntry*)Unsafe.AsPointer(in TypeInterfaceEntriesImpl.Entries);
    }

    /// <inheritdoc/>
    public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        wrapperFlags = CreatedWrapperFlags.NonWrapping;

        Type abi = WindowsRuntimeValueTypeMarshaller.UnboxToManagedUnsafe<Type>(value, in WellKnownWindowsInterfaceIIDs.IID_IReferenceOfType);

        // Try to marshal the resulting type (it might not actually succeed)
        global::System.Type? type = TypeMarshaller.ConvertToManaged(abi);

        // Because this is the callback from 'ComWrappers', we're not allowed to return 'null'.
        // So if we fail to resolve a value entirely, we just throw directy from here instead.
        if (type is null)
        {
            TypeExceptions.ThrowArgumentExceptionForNullType(abi);
        }

        return type;
    }
}

/// <summary>
/// Binding type for the <c>IReference`1</c> implementation for <see cref="global::System.Type"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
file unsafe struct TypeReferenceVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, Type*, HRESULT> get_Value;
}

/// <summary>
/// The <c>IReference`1</c> implementation for <see cref="global::System.Type"/>.
/// </summary>
file static unsafe class TypeReferenceImpl
{
    /// <summary>
    /// The <see cref="TypeReferenceVftbl"/> value for the managed <c>IReference`1</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly TypeReferenceVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static TypeReferenceImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_Value = &get_Value;
    }

    /// <summary>
    /// Gets a pointer to the managed <c>IReference`1</c> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ireference-1.value"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_Value(void* thisPtr, Type* result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            global::System.Type thisObject = ComInterfaceDispatch.GetInstance<global::System.Type>((ComInterfaceDispatch*)thisPtr);

            *result = TypeMarshaller.ConvertToUnmanaged(thisObject);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

/// <summary>
/// A custom <see cref="TypeInfo"/> implementation for metadata types that are missing metadata information.
/// </summary>
file sealed class NoMetadataTypeInfo : TypeInfo
{
    /// <summary>
    /// The full name of the type missing metadata information.
    /// </summary>
    private readonly string _fullName;

    /// <summary>
    /// Creates a new <see cref="NoMetadataTypeInfo"/> instance with the specified parameters.
    /// </summary>
    /// <param name="fullName">The full name of the type missing metadata information.</param>
    public NoMetadataTypeInfo(string fullName)
    {
        _fullName = fullName;
    }

    /// <inheritdoc/>
    public override Assembly Assembly => throw new NotSupportedException();

    /// <inheritdoc/>
    public override string AssemblyQualifiedName => throw new NotSupportedException();

    /// <inheritdoc/>
    public override global::System.Type BaseType => throw new NotSupportedException();

    /// <inheritdoc/>
    public override string FullName => _fullName;

    /// <inheritdoc/>
    public override Guid GUID => throw new NotSupportedException();

    /// <inheritdoc/>
    public override Module Module => throw new NotSupportedException();

    /// <inheritdoc/>
    public override string Namespace => throw new NotSupportedException();

    /// <inheritdoc/>
    public override global::System.Type UnderlyingSystemType => throw new NotSupportedException();

    /// <inheritdoc/>
    public override string Name => throw new NotSupportedException();

    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2094", Justification = "This method will always throw.")]
    public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    public override object[] GetCustomAttributes(bool inherit)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    public override object[] GetCustomAttributes(global::System.Type attributeType, bool inherit)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    public override global::System.Type GetElementType()
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2094", Justification = "This method will always throw.")]
    public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2094", Justification = "This method will always throw.")]
    public override EventInfo[] GetEvents(BindingFlags bindingAttr)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2094", Justification = "This method will always throw.")]
    public override FieldInfo GetField(string name, BindingFlags bindingAttr)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2094", Justification = "This method will always throw.")]
    public override FieldInfo[] GetFields(BindingFlags bindingAttr)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2093", Justification = "This method will always throw.")]
    [UnconditionalSuppressMessage("Trimming", "IL2094", Justification = "This method will always throw.")]
    public override global::System.Type GetInterface(string name, bool ignoreCase)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2094", Justification = "This method will always throw.")]
    public override global::System.Type[] GetInterfaces()
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2094", Justification = "This method will always throw.")]
    public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2094", Justification = "This method will always throw.")]
    public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2094", Justification = "This method will always throw.")]
    public override global::System.Type GetNestedType(string name, BindingFlags bindingAttr)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2094", Justification = "This method will always throw.")]
    public override global::System.Type[] GetNestedTypes(BindingFlags bindingAttr)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2094", Justification = "This method will always throw.")]
    public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2094", Justification = "This method will always throw.")]
    public override object InvokeMember(
        string name,
        BindingFlags invokeAttr,
        Binder? binder,
        object? target,
        object?[]? args,
        ParameterModifier[]? modifiers,
        CultureInfo? culture,
        string[]? namedParameters)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    public override bool IsDefined(global::System.Type attributeType, bool inherit)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    protected override TypeAttributes GetAttributeFlagsImpl()
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2094", Justification = "This method will always throw.")]
    protected override ConstructorInfo GetConstructorImpl(
        BindingFlags bindingAttr,
        Binder? binder,
        CallingConventions callConvention,
        global::System.Type[]? types,
        ParameterModifier[]? modifiers)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2094", Justification = "This method will always throw.")]
    protected override MethodInfo GetMethodImpl(
        string name,
        BindingFlags bindingAttr,
        Binder? binder,
        CallingConventions callConvention,
        global::System.Type[]? types,
        ParameterModifier[]? modifiers)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    [UnconditionalSuppressMessage("Trimming", "IL2094", Justification = "This method will always throw.")]
    protected override PropertyInfo GetPropertyImpl(
        string name,
        BindingFlags bindingAttr,
        Binder? binder,
        global::System.Type? returnType,
        global::System.Type[]? types,
        ParameterModifier[]? modifiers)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    protected override bool HasElementTypeImpl()
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    protected override bool IsArrayImpl()
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    protected override bool IsByRefImpl()
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    protected override bool IsCOMObjectImpl()
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    protected override bool IsPointerImpl()
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    protected override bool IsPrimitiveImpl()
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return _fullName;
    }

    /// <inheritdoc/>
    public override bool Equals(object? o)
    {
        return ReferenceEquals(this, o);
    }

    /// <inheritdoc/>
    public override bool Equals(global::System.Type? o)
    {
        return ReferenceEquals(this, o);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return _fullName.GetHashCode();
    }
}
#endif