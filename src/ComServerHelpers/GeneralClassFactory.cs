// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.Versioning;

namespace ComServerHelpers;

/// <summary>
/// General class factory for .NET types using generics.
/// </summary>
/// <typeparam name="T">Type the factory creates.</typeparam>
/// <typeparam name="TInterface">Interface that <typeparamref name="T"/> implements.</typeparam>
/// <seealso cref="BaseClassFactory"/>
[SupportedOSPlatform("windows6.0.6000")]
public sealed class GeneralClassFactory<T, TInterface> : BaseClassFactory where T : class, TInterface, new()
{
    /// <inheritdoc/>
    protected internal override Guid Clsid => typeof(T).GUID;

    /// <inheritdoc/>
    protected internal override Guid Iid => typeof(TInterface).GUID;

    /// <inheritdoc/>
    protected internal override object CreateInstance()
    {
        return new T();
    }
}
