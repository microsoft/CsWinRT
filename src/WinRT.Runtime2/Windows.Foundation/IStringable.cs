// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
using Windows.Foundation.Metadata;
#endif
using WindowsRuntime;

namespace Windows.Foundation;

/// <summary>
/// Provides a way to represent the current object as a <see cref="string"/>.
/// </summary>
/// <remarks>
/// Managed types should not implement the <see cref="IStringable"/> interface. Rather, they should override the
/// <see cref="object.ToString"/> method. When exposed to native code, they will implicitly get an implementation
/// of <see cref="IStringable"/> that will call the managed <see cref="object.ToString"/> override.
/// </remarks>
[Guid("96369F54-8EB6-48F0-ABCE-C1B211E627C3")]
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
[ContractVersion(typeof(FoundationContract), 65536u)]
#elif WINDOWS_RUNTIME_IMPLEMENTATION_ASSEMBLY
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
#endif
public interface IStringable
{
    /// <summary>
    /// Gets a <see cref="string"/> that represents the current object.
    /// </summary>
    /// <returns>The <see cref="string"/> representation of the current object.</returns>
    string ToString();
}