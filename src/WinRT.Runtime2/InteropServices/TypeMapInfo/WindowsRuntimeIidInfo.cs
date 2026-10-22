// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Input;

#pragma warning disable IDE0046

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A type providing cached IID information independently of marshalling and vtables.
/// </summary>
internal sealed class WindowsRuntimeIidInfo
{
    /// <summary>
    /// The external map of type names to IID metadata providers.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Entries are conditional on referenced types, not allocated types. Only IID lookups use this group.")]
    private static readonly IReadOnlyDictionary<string, Type> ExternalTypeMapping = TypeMapping.GetOrCreateExternalTypeMapping<WindowsRuntimeIidTypeMapGroup>();

    /// <summary>
    /// Cached IID information for types, including unsupported types.
    /// </summary>
    private static readonly ConditionalWeakTable<Type, WindowsRuntimeIidInfo> TypeToIidInfoTable = [];

    /// <summary>
    /// Cached creation factory for <see cref="CreateIidInfo"/>.
    /// </summary>
    private static readonly ConditionalWeakTable<Type, WindowsRuntimeIidInfo>.CreateValueCallback CreateIidInfoCallback = new(CreateIidInfo);

    /// <summary>
    /// The managed type associated with this instance.
    /// </summary>
    private readonly Type _type;

    /// <summary>
    /// The IID, or <see langword="null"/> for a type without a Windows Runtime interface.
    /// </summary>
    private readonly Guid? _iid;

    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeIidInfo"/> instance with the specified parameters.
    /// </summary>
    /// <param name="type">The managed type.</param>
    /// <param name="iid">The associated IID, if any.</param>
    private WindowsRuntimeIidInfo(Type type, Guid? iid = null)
    {
        _type = type;
        _iid = iid;
    }

    /// <summary>
    /// Gets a <see cref="WindowsRuntimeIidInfo"/> instance for a managed type.
    /// </summary>
    /// <param name="type">The managed type to look up.</param>
    /// <returns>The cached IID information.</returns>
    public static WindowsRuntimeIidInfo GetInfo(Type type)
    {
        return TypeToIidInfoTable.GetValue(type, CreateIidInfoCallback);
    }

    /// <summary>
    /// Gets the IID associated with this instance.
    /// </summary>
    /// <returns>The Windows Runtime interface identifier.</returns>
    /// <exception cref="NotSupportedException">Thrown if the type has no associated Windows Runtime IID.</exception>
    public Guid GetIid()
    {
        if (_iid is Guid iid)
        {
            return iid;
        }

        [DoesNotReturn]
        [StackTraceHidden]
        void ThrowNotSupportedException()
        {
            throw new NotSupportedException(
                $"The type '{_type}' does not have a precomputed Windows Runtime IID. " +
                "Value metadata names do not imply an interface IID. IReference<T> requires a nullable Windows Runtime value type. " +
                "Static runtime classes and Windows Runtime attributes have no native interface. " +
                "Constructed generic and array types must be discoverable when the interop assembly is generated.");
        }

        ThrowNotSupportedException();

        return default;
    }

    /// <summary>
    /// Creates IID information using a precomputed provider from the external type map.
    /// </summary>
    /// <param name="type">The managed type.</param>
    /// <returns>The IID information, including a cached negative result for unsupported types.</returns>
    private static WindowsRuntimeIidInfo CreateIidInfo(Type type)
    {
        return ExternalTypeMapping.TryGetValue(WindowsRuntimeIidTypeName.GetName(type), out Type? provider)
            ? new(type, provider.GUID)
            : new(type);
    }
}

/// <summary>
/// Formats IID map keys using logical System identities and assembly names for all other types.
/// </summary>
file static class WindowsRuntimeIidTypeName
{
    /// <summary>
    /// Gets the key for a closed managed type.
    /// </summary>
    /// <param name="type">The type to format.</param>
    /// <returns>The key in <c>[Assembly]Namespace.Type[arguments]</c> format.</returns>
    public static string GetName(Type type)
    {
        static void Append(ref DefaultInterpolatedStringHandler handler, Type type)
        {
            if (type.IsSZArray)
            {
                Append(ref handler, type.GetElementType()!);
                handler.AppendLiteral("[]");

                return;
            }

            Type definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
            bool isSystemType = IsSystemType(definition);
            string assemblyName = isSystemType ? "#System" : definition.Assembly.GetName().Name!;

            handler.AppendFormatted('[');

            // Literal assembly names cannot occupy the reserved logical scope
            if (!isSystemType && assemblyName.StartsWith('#'))
            {
                handler.AppendFormatted('#');
            }

            handler.AppendLiteral(assemblyName);
            handler.AppendFormatted(']');
            handler.AppendLiteral(definition.FullName!);

            if (type.IsGenericType)
            {
                handler.AppendFormatted('[');

                Type[] arguments = type.GenericTypeArguments;

                for (int index = 0; index < arguments.Length; index++)
                {
                    if (index != 0)
                    {
                        handler.AppendFormatted(',');
                    }

                    Append(ref handler, arguments[index]);
                }

                handler.AppendFormatted(']');
            }
        }

        DefaultInterpolatedStringHandler handler = new(0, 0, null, stackalloc char[256]);

        Append(ref handler, type);

        return handler.ToStringAndClear();
    }

    /// <summary>
    /// Identifies the fundamental and custom-mapped System definitions independently of their assembly placement.
    /// </summary>
    /// <param name="type">The non-generic type or generic type definition.</param>
    /// <returns>Whether the type uses the reserved logical System scope.</returns>
    /// <remarks>Keep aligned with the existing mapped-type classifiers used by WindowsRuntimeIidTypeNameFactory.</remarks>
    private static bool IsSystemType(Type type)
    {
        // Keep the 'typeof' references confined to comparisons so Native AOT need not retain their reflection metadata
        // Storing these 'Type' objects in a static table would root that metadata instead
        return
            type == typeof(bool) ||
            type == typeof(byte) ||
            type == typeof(char) ||
            type == typeof(double) ||
            type == typeof(float) ||
            type == typeof(short) ||
            type == typeof(int) ||
            type == typeof(long) ||
            type == typeof(ushort) ||
            type == typeof(uint) ||
            type == typeof(ulong) ||
            type == typeof(Guid) ||
            type == typeof(object) ||
            type == typeof(string) ||
            type == typeof(TimeSpan) ||
            type == typeof(DateTimeOffset) ||
            type == typeof(Exception) ||
            type == typeof(Type) ||
            type == typeof(Uri) ||
            type == typeof(Matrix3x2) ||
            type == typeof(Matrix4x4) ||
            type == typeof(Plane) ||
            type == typeof(Quaternion) ||
            type == typeof(Vector2) ||
            type == typeof(Vector3) ||
            type == typeof(Vector4) ||
            type == typeof(DataErrorsChangedEventArgs) ||
            type == typeof(PropertyChangedEventArgs) ||
            type == typeof(NotifyCollectionChangedAction) ||
            type == typeof(NotifyCollectionChangedEventArgs) ||
            type == typeof(IDisposable) ||
            type == typeof(IServiceProvider) ||
            type == typeof(ICommand) ||
            type == typeof(IEnumerable) ||
            type == typeof(IEnumerator) ||
            type == typeof(IList) ||
            type == typeof(INotifyCollectionChanged) ||
            type == typeof(INotifyDataErrorInfo) ||
            type == typeof(INotifyPropertyChanged) ||
            type == typeof(IEnumerable<>) ||
            type == typeof(IEnumerator<>) ||
            type == typeof(IList<>) ||
            type == typeof(IReadOnlyList<>) ||
            type == typeof(IDictionary<,>) ||
            type == typeof(IReadOnlyDictionary<,>) ||
            type == typeof(EventHandler<>) ||
            type == typeof(EventHandler<,>) ||
            type == typeof(NotifyCollectionChangedEventHandler) ||
            type == typeof(PropertyChangedEventHandler) ||
            type == typeof(EventHandler) ||
            type == typeof(Nullable<>) ||
            type == typeof(KeyValuePair<,>);
    }
}
