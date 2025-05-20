using System;
using System.Runtime.Versioning;

namespace ComServerHelpers;

/// <summary>
/// Delegate based activation factory for .NET types.
/// </summary>
/// <typeparam name="T">The type the factory creates.</typeparam>
/// <param name="factory">Delegate to create instances.</param>
/// <seealso cref="BaseActivationFactory"/>
[SupportedOSPlatform("windows8.0")]
public sealed class DelegateActivationFactory<T>(Func<T> factory) : BaseActivationFactory where T : class
{
    private readonly Func<T> factory = factory;

    /// <inheritdoc/>
    public override string ActivatableClassId => typeof(T).FullName ?? throw new InvalidOperationException($"Unable to get activation class ID for type {typeof(T)}");

    /// <inheritdoc/>
    public override object ActivateInstance()
    {
        return factory();
    }
}
