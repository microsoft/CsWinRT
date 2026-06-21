// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if CSWINRT_REFERENCE_PROJECTION
#pragma warning disable CSWINRT3004 // "Type or member '...' is a private implementation detail"
[assembly: WindowsRuntime.InteropServices.WindowsRuntimeReferenceAssembly]
#else
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: DisableRuntimeMarshallingAttribute]
[assembly: AssemblyMetadata("IsTrimmable", "True")]
[assembly: AssemblyMetadata("IsAotCompatible", "True")]
#endif