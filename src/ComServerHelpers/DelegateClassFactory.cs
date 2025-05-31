// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.Versioning;

namespace ComServerHelpers;

/// <summary>
/// Delegate based class factory for .NET types.
/// </summary>
/// <typeparam name="T">Type the factory creates.</typeparam>
/// <typeparam name="TInterface">Interface that <typeparamref name="T"/> implements.</typeparam>
/// <param name="factory">Delegate to create instances.</param>
/// <seealso cref="BaseClassFactory"/>
[SupportedOSPlatform("windows6.0.6000")]
public sealed class DelegateClassFactory<T, TInterface>(Func<T> factory) : BaseClassFactory where T : class, TInterface
{
    // State
    private readonly Func<T> factory = factory;

    // Properties (special values)
    /// <inheritdoc/>
    protected internal override Guid Clsid => typeof(T).GUID;

    /// <inheritdoc/>
    protected internal override Guid Iid => typeof(TInterface).GUID;

    // Other members (methods)
    /// <inheritdoc/>
    protected internal override object CreateInstance()
    {
        return factory();
    }
}
