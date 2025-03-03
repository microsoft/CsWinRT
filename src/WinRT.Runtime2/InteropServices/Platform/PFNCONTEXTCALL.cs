// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// Using 'void*' as a temporary workaround for https://github.com/dotnet/roslyn/issues/77389.
// global using unsafe PFNCONTEXTCALL = delegate* unmanaged<WindowsRuntime.InteropServices.ComCallData*, int>;
global using unsafe PFNCONTEXTCALL = void*;
