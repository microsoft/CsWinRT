// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY

#pragma warning disable IDE0060, IDE1006

namespace WindowsRuntime.InteropServices;

/// <summary>
/// The <c>IPropertyValue</c> implementation for managed types.
/// </summary>
/// <remarks>
/// Unlike other "Impl" types, <c>IPropertyValue</c> is implemented in a specialized manner on different types.
/// This type provides shared paths for some implementations, and then some specific full implementations.
/// </remarks>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue"/>
public static partial class IPropertyValueImpl;
#endif
