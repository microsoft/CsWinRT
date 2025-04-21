// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.UI.Xaml.Interop;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

#pragma warning disable CS0649

[assembly: TypeMap<WindowsRuntimeTypeMapUniverse>(
    value: "Windows.Foundation.IReference<Windows.UI.Xaml.Interop.TypeName>",
    target: typeof(ABI.System.Type),
    trimTarget: typeof(Type))]

[assembly: TypeMapAssociation<WindowsRuntimeTypeMapUniverse>(typeof(Type), typeof(ABI.System.Type))]

namespace ABI.System;

/// <summary>
/// ABI type for <see cref="global::System.Type"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.typename"/>
[EditorBrowsable(EditorBrowsableState.Never)]
[WindowsRuntimeClassName("Windows.Foundation.IReference<Windows.UI.Xaml.Interop.TypeName>")]
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
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class TypeMarshaller
{
    /// <summary>
    /// Converts a managed <see cref="global::System.Type"/> to an unmanaged <see cref="Type"/>.
    /// </summary>
    /// <param name="value">The managed <see cref="global::System.Type"/> value.</param>
    /// <returns>The unmanaged <see cref="Type"/> value.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <see langword="null"/>.</exception>"
    public static Type ConvertToUnmanaged(global::System.Type value)
    {
        ArgumentNullException.ThrowIfNull(value);

        // TODO
        return default;
    }

    /// <summary>
    /// Converts an unmanaged <see cref="Type"/> to a managed <see cref="global::System.Type"/>.
    /// </summary>
    /// <param name="value">The unmanaged <see cref="Type"/> value.</param>
    /// <returns>The managed <see cref="global::System.Type"/> value</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "Any types which are trimmed are not used by managed user code and there is fallback logic to handle that.")]
    public static global::System.Type? ConvertToManaged(Type value)
    {
        ReadOnlySpan<char> typeName = HStringMarshaller.ConvertToManagedUnsafe(value.Name);

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

        global::System.Type? type = null; // TODO

        // If the target type is a projected type that has been trimmed, we can return a special type.
        // This is mostly used by the XAML metadata provider. The type itself should never actually be
        // used, as there's no C# references causing it to be rooted. If anyone tried to use it, the
        // returned implementation will just throw an exception for all unsupported operations on it.
        if (type is null && value.Kind is TypeKind.Metadata)
        {
            return NoMetadataTypeInfo.GetOrCreate(typeName.ToString());
        }

        // Return whatever result we managed to get from the cache
        return type;
    }

    /// <inheritdoc cref="WindowsRuntimeValueTypeMarshaller.BoxToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue BoxToUnmanaged(global::System.Type? value)
    {
        return value is null ? default : new((void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.None, in WellKnownInterfaceIds.IID_IReferenceOfType));
    }

    /// <inheritdoc cref="WindowsRuntimeValueTypeMarshaller.UnboxToManaged(void*)"/>
    public static global::System.Type? UnboxToManaged(void* value)
    {
        Type? abi = WindowsRuntimeValueTypeMarshaller.UnboxToManaged<Type>(value);

        return abi.HasValue ? ConvertToManaged(abi.Value) : null;
    }
}

/// <summary>
/// The set of <see cref="ComInterfaceEntry"/> values for <see cref="global::System.Type"/>.
/// </summary>
file struct TypeInterfaceEntries
{
    public ComInterfaceEntry IReferenceOfType;
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
        Entries.IReferenceOfType.IID = WellKnownInterfaceIds.IID_IReferenceOfType;
        Entries.IReferenceOfType.Vtable = TypeReferenceImpl.Vtable;
        Entries.IStringable.IID = WellKnownInterfaceIds.IID_IStringable;
        Entries.IStringable.Vtable = IStringableImpl.Vtable;
        Entries.IWeakReferenceSource.IID = WellKnownInterfaceIds.IID_IWeakReferenceSource;
        Entries.IWeakReferenceSource.Vtable = IWeakReferenceSourceImpl.Vtable;
        Entries.IMarshal.IID = WellKnownInterfaceIds.IID_IMarshal;
        Entries.IMarshal.Vtable = IMarshalImpl.Vtable;
        Entries.IAgileObject.IID = WellKnownInterfaceIds.IID_IAgileObject;
        Entries.IAgileObject.Vtable = IUnknownImpl.Vtable;
        Entries.IInspectable.IID = WellKnownInterfaceIds.IID_IInspectable;
        Entries.IInspectable.Vtable = IInspectableImpl.Vtable;
        Entries.IUnknown.IID = WellKnownInterfaceIds.IID_IUnknown;
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
        return (void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.None);
    }

    /// <inheritdoc/>
    public override ComInterfaceEntry* ComputeVtables(out int count)
    {
        count = sizeof(TypeInterfaceEntries) / sizeof(ComInterfaceEntry);

        return (ComInterfaceEntry*)Unsafe.AsPointer(ref Unsafe.AsRef(in TypeInterfaceEntriesImpl.Entries));
    }

    /// <inheritdoc/>
    public override object CreateObject(void* value)
    {
        Type abi = WindowsRuntimeValueTypeMarshaller.UnboxToManagedUnsafe<Type>(value, in WellKnownInterfaceIds.IID_IReferenceOfType);

        // Try to marshal the resulting type (it might not actually succeed)
        global::System.Type? type = TypeMarshaller.ConvertToManaged(abi);

        // Because this is the callback from 'ComWrappers', we're not allowed to return 'null'.
        // So if we fail to resolve a value entirely, we just throw directy from here instead.
        if (type is null)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowArgumentException(Type abi)
            {
                throw new NotSupportedException(
                    $"The type with name '{HStringMarshaller.ConvertToManaged(abi.Name)}' and kind '{abi.Kind}' cannot cannot be marshalled to a managed 'Type' instance. " +
                    $"If the application is running with trimming enabled (or on Native AOT), it's possible the issue is caused by trimming causing all metadata for the type " +
                    $"to be removed. To work around the issue, consider using the '[DynamicDependency]' attribute over the method causing this exception to eventually be thrown. " +
                    $"You can see the API docs for this attribute here: https://learn.microsoft.com/dotnet/api/system.diagnostics.codeanalysis.dynamicdependencyattribute.");
            }

            ThrowArgumentException(abi);
        }

        return type;
    }
}

/// <summary>
/// Binding type for the <c>IReference`1</c> implementation for <see cref="global::System.Type"/>.
/// </summary>
file unsafe struct TypeReferenceVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, Type*, HRESULT> Value;
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

        Vftbl.Value = &Value;
    }

    /// <summary>
    /// Gets a pointer to the managed <c>IReference`1</c> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(ref Unsafe.AsRef(in Vftbl));
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ireference-1.value"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT Value(void* thisPtr, Type* result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            global::System.Type unboxedValue = ComInterfaceDispatch.GetInstance<global::System.Type>((ComInterfaceDispatch*)thisPtr);

            Unsafe.WriteUnaligned(result, TypeMarshaller.ConvertToUnmanaged(unboxedValue));

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            Unsafe.WriteUnaligned(result, default(TimeSpan));

            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

/// <summary>
/// A custom <see cref="TypeInfo"/> implementation for metadata types that are missing metadata information.
/// </summary>
file sealed class NoMetadataTypeInfo : TypeInfo
{
    private static readonly ConcurrentDictionary<string, NoMetadataTypeInfo> FakeMetadataTypeCache = new(StringComparer.Ordinal);

    /// <summary>
    /// The full name of the type missing metadata information.
    /// </summary>
    private readonly string _fullName;

    /// <summary>
    /// Creates a new <see cref="NoMetadataTypeInfo"/> instance with the specified parameters.
    /// </summary>
    /// <param name="fullName">The full name of the type missing metadata information.</param>
    private NoMetadataTypeInfo(string fullName)
    {
        _fullName = fullName;
    }

    /// <summary>
    /// Gets a cached <see cref="NoMetadataTypeInfo"/> instance for the specified type name.
    /// </summary>
    /// <param name="fullName">The full name of the type missing metadata information.</param>
    /// <returns>The resulting <see cref="NoMetadataTypeInfo"/> instance.</returns>
    public static NoMetadataTypeInfo GetOrCreate(string fullName)
    {
        return FakeMetadataTypeCache.GetOrAdd(fullName, static (name) => new NoMetadataTypeInfo(name));
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
