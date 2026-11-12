// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A dummy placeholder type to represent some reference type, to be used as a type argument.
/// </summary>
/// <remarks>
/// This can be used instead of <see cref="ValueTypePlaceholder"/> when a reference type is
/// required. In case of an actual instantiation (e.g. through <see langword="default"/>),
/// using <see cref="ValueTypePlaceholder"/> offers better performance.
/// </remarks>
internal abstract class ReferenceTypePlaceholder;