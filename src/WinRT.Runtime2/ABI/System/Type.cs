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
using Windows.Foundation;
using Windows.UI.Xaml.Interop;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

#pragma warning disable IDE1006, CA1416

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
[assembly: TypeMap<WindowsRuntimeComWrappersTypeMapGroup>(
    value: "Windows.Foundation.IReference<Windows.UI.Xaml.Interop.TypeName>",
    target: typeof(ABI.System.Type),
    trimTarget: typeof(Type))]
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code

[assembly: TypeMapAssociation<WindowsRuntimeComWrappersTypeMapGroup>(typeof(Type), typeof(ABI.System.Type))]

namespace ABI.System;

/// <summary>
/// ABI type for <see cref="global::System.Type"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.typename"/>
[WindowsRuntimeClassName("Windows.Foundation.IReference<Windows.UI.Xaml.Interop.TypeName>")]
[TypeComWrappersMarshaller]
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
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
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
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
        reference = default;
        if (value is NoMetadataTypeInfo noMetadataTypeInfo)
        {
            reference = new TypeReference { Name = noMetadataTypeInfo.FullName, Kind = TypeKind.Metadata };
            return;
        }

        if (value is not null)
        {
            TypeKind kind = TypeKind.Metadata;
            if (value.IsPrimitive)
            {
                kind = TypeKind.Primitive;
            }
            if (WindowsRuntimeMarshallingInfo.TryGetInfo(value, out WindowsRuntimeMarshallingInfo? marshallingInfo))
            {
                if (marshallingInfo.GetIsProxyType())
                {
                    reference = new TypeReference { Name = value.AssemblyQualifiedName, Kind = TypeKind.Custom };
                    return;
                }
                reference = new TypeReference { Name = ExtractTypeName(marshallingInfo.GetRuntimeClassName()), Kind = kind };
                return;
            }

            reference = new TypeReference { Name = value.AssemblyQualifiedName, Kind = TypeKind.Custom };
        }
    }


    /// <summary>
    /// Private method to extracts the generic type argument from a runtime class name that follows
    /// the pattern <c>Windows.Foundation.IReference&lt;T&gt;</c>.
    /// </summary>
    /// <param name="runtimeClassName">
    /// The full runtime class name, e.g., <c>Windows.Foundation.IReference&lt;System.Int32&gt;</c>.
    /// </param>
    /// <returns>
    /// The inner type name if the input matches the expected pattern; otherwise, the original string.
    /// </returns>
    private static string ExtractTypeName(string runtimeClassName)
    {
        const string prefix = "Windows.Foundation.IReference<";
        ReadOnlySpan<char> span = runtimeClassName;
        return span.StartsWith(prefix) ? span.Slice(prefix.Length, span.Length - prefix.Length - 1).ToString() : runtimeClassName;
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

        global::System.Type? type = null;
        if (WindowsRuntimeMarshallingInfo.TryGetInfo(typeName, out WindowsRuntimeMarshallingInfo? marshallingInfo))
        {
            type = marshallingInfo.PublicType;
        }

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
            global::System.Type unboxedValue = ComInterfaceDispatch.GetInstance<global::System.Type>((ComInterfaceDispatch*)thisPtr);

            *result = TypeMarshaller.ConvertToUnmanaged(unboxedValue);

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