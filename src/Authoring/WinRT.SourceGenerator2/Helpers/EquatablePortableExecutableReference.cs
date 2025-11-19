// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace WindowsRuntime.SourceGenerator;

/// <summary>
/// An equatable <see cref="PortableExecutableReference"/> type that weakly references a <see cref="Microsoft.CodeAnalysis.Compilation"/> object.
/// </summary>
/// <param name="executableReference">The <see cref="PortableExecutableReference"/> object to wrap.</param>
/// <param name="compilation">The <see cref="Microsoft.CodeAnalysis.Compilation"/> instance where <paramref name="executableReference"/> comes from.</param>
internal sealed class EquatablePortableExecutableReference(
    PortableExecutableReference executableReference,
    Compilation compilation) : IEquatable<EquatablePortableExecutableReference>
{
    /// <summary>
    /// A weak reference to the <see cref="Microsoft.CodeAnalysis.Compilation"/> object owning <see cref="Reference"/>.
    /// </summary>
    private readonly WeakReference<Compilation> Compilation = new(compilation);

    /// <summary>
    /// Gets the <see cref="PortableExecutableReference"/> object for this instance.
    /// </summary>
    public PortableExecutableReference Reference { get; } = executableReference;

    /// <summary>
    /// Gets the <see cref="Microsoft.CodeAnalysis.Compilation"/> object for <see cref="Reference"/>.
    /// </summary>
    /// <returns>The <see cref="Microsoft.CodeAnalysis.Compilation"/> object for <see cref="Reference"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="Microsoft.CodeAnalysis.Compilation"/> object has been collected.</exception>
    /// <remarks>
    /// This method should only be used from incremental steps immediately following a change in the metadata reference
    /// being used, as that would guarantee that that <see cref="Microsoft.CodeAnalysis.Compilation"/> object would be alive.
    /// </remarks>
    public Compilation GetCompilationUnsafe()
    {
        return Compilation.TryGetTarget(out Compilation? compilation)
            ? compilation
            : throw new InvalidOperationException("No compilation object is available.");
    }

    /// <inheritdoc/>
    public override bool Equals(object? other)
    {
        return Equals(other as EquatablePortableExecutableReference);
    }

    /// <inheritdoc/>
    public bool Equals([NotNullWhen(true)] EquatablePortableExecutableReference? other)
    {
        try
        {
            return other?.Reference.GetMetadataId() == Reference.GetMetadataId();
        }
        catch
        {
            // 'GetMetadataId' can throws in some rare cases, so just handle that explicitly
            return false;
        }
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        try
        {
            return Reference.GetMetadataId().GetHashCode();
        }
        catch
        {
            // If we throw, we just always use '0'. The 'Equals' calls after it will return 'false' anyway.
            return 0;
        }
    }
}