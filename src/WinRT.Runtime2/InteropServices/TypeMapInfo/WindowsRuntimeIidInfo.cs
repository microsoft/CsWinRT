// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
        if (type.ContainsGenericParameters || type.FullName is null || (type.IsArray && (!type.IsSZArray || type.GetElementType()!.IsArray)))
        {
            return new(type);
        }

        if (type.IsAssignableTo(typeof(Exception)) && type != typeof(Exception))
        {
            return new(type, GetInfo(typeof(Exception))._iid);
        }

        if (type.IsAssignableTo(typeof(Type)) && type != typeof(Type))
        {
            return new(type, GetInfo(typeof(Type))._iid);
        }

        if (Nullable.GetUnderlyingType(type) is Type underlyingType)
        {
            if (underlyingType.IsGenericType && underlyingType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                return new(type);
            }

            return new(type, GetInfo(underlyingType)._iid);
        }

        return ExternalTypeMapping.TryGetValue(WindowsRuntimeIidTypeName.GetName(type), out Type? provider)
            ? new(type, provider.GUID)
            : new(type);
    }
}

/// <summary>
/// Formats IID map keys without assembly versions, cultures, or public key tokens.
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
            if (type.IsArray)
            {
                Append(ref handler, type.GetElementType()!);
                handler.AppendLiteral("[]");

                return;
            }

            handler.AppendFormatted('[');
            handler.AppendLiteral(type.Assembly.GetName().Name!);
            handler.AppendFormatted(']');
            handler.AppendLiteral((type.IsGenericType ? type.GetGenericTypeDefinition().FullName : type.FullName)!);

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

        DefaultInterpolatedStringHandler handler = new(0, 0, null, initialBuffer: stackalloc char[256]);

        Append(ref handler, type);

        return handler.ToStringAndClear();
    }
}
