// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using WindowsRuntime.InteropServices.Marshalling;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A Windows Runtime .dll module, used to activate types with manifest-free activation.
/// </summary>
internal sealed unsafe class WindowsRuntimeDllModule
{
    /// <summary>
    /// The base directory for the current application.
    /// </summary>
    private static readonly string ApplicationBaseDirectory = AppContext.BaseDirectory;

    /// <summary>
    /// The cache of loaded .dll modules.
    /// </summary>
    private static readonly Dictionary<string, WindowsRuntimeDllModule> LoadedModuleCache = new(StringComparer.Ordinal);

    /// <summary>
    /// The lock to synchronize accesses to <see cref="LoadedModuleCache"/>.
    /// </summary>
    private static readonly Lock LoadedModuleCacheLock = new();

    /// <summary>
    /// The name of the .dll module.
    /// </summary>
    private readonly string _fileName;

    /// <summary>
    /// The handle to the loaded .dll module.
    /// </summary>
    private readonly HANDLE _moduleHandle;

    /// <summary>
    /// The delegate for the <c>DllGetActivationFactory</c> function for the current module.
    /// </summary>
    /// <see href="https://learn.microsoft.com/previous-versions/br205771(v=vs.85)"/>
    private readonly delegate* unmanaged[Stdcall]<HSTRING, void**, HRESULT> _dllGetActivationFactory;

    /// <summary>
    /// The delegate for the <c>DllCanUnloadNow</c> function for the current module.
    /// </summary>
    /// <remarks>
    /// This should eventually be called periodically, to allow unloading libraries using manifest-free activation.
    /// </remarks>
    /// <see href="https://learn.microsoft.com/windows/win32/api/combaseapi/nf-combaseapi-dllcanunloadnow"/>
    private readonly delegate* unmanaged[Stdcall]<HRESULT> _dllCanUnloadNow;

    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeDllModule"/> instance with the specified parameters.
    /// </summary>
    /// <param name="fileName"><inheritdoc cref="_fileName" path="/summary/node()"/></param>
    /// <param name="moduleHandle"><inheritdoc cref="_moduleHandle" path="/summary/node()"/></param>
    /// <param name="dllGetActivationFactory"><inheritdoc cref="_dllGetActivationFactory" path="/summary/node()"/></param>
    /// <param name="dllCanUnloadNow"><inheritdoc cref="_dllCanUnloadNow" path="/summary/node()"/></param>
    private WindowsRuntimeDllModule(
        string fileName,
        HANDLE moduleHandle,
        delegate* unmanaged[Stdcall]<HSTRING, void**, HRESULT> dllGetActivationFactory,
        delegate* unmanaged[Stdcall]<HRESULT> dllCanUnloadNow)
    {
        _fileName = fileName;
        _moduleHandle = moduleHandle;
        _dllGetActivationFactory = dllGetActivationFactory;
        _dllCanUnloadNow = dllCanUnloadNow;
    }

    /// <summary>
    /// Unloads the .dll module for the current instance.
    /// </summary>
    /// <exception cref="Win32Exception"></exception>
    ~WindowsRuntimeDllModule()
    {
        Debug.Assert(_dllCanUnloadNow == null || WellKnownErrorCodes.Succeeded(_dllCanUnloadNow()));

        if ((_moduleHandle != (HANDLE)null) && !WellKnownErrorCodes.Succeeded(WindowsRuntimeImports.FreeLibrary(_moduleHandle)))
        {
            // The 'Win32Exception' constructor will automatically get the last system error
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowWin32Exception() => throw new Win32Exception();

            ThrowWin32Exception();
        }
    }

    /// <summary>
    /// Tries to load a <see cref="WindowsRuntimeDllModule"/> with a specified name.
    /// </summary>
    /// <param name="fileName">The name of the .dll module to load.</param>
    /// <param name="module">The resulting .dll module.</param>
    /// <returns>Whether loading the module was successful.</returns>
    public static bool TryLoad(string fileName, [NotNullWhen(true)] out WindowsRuntimeDllModule? module)
    {
        lock (LoadedModuleCacheLock)
        {
            // Check if the module is already loaded, just reuse it if so
            if (LoadedModuleCache.TryGetValue(fileName, out module))
            {
                return true;
            }

            // Try to load the module, and add it to the cache if that succeeds
            if (TryCreate(fileName, out module))
            {
                LoadedModuleCache[fileName] = module;

                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Tries to activate a Windows Runtime type from the current module.
    /// </summary>
    /// <param name="runtimeClassName">The runtime class name for the type to activate.</param>
    /// <param name="activationFactory">The resulting <see cref="WindowsRuntimeObjectReference"/> instance.</param>
    /// <returns>The <c>HRESULT</c> for the operation.</returns>
    public HRESULT GetActivationFactory(string runtimeClassName, out WindowsRuntimeObjectReference? activationFactory)
    {
        HRESULT hresult = GetActivationFactoryUnsafe(runtimeClassName, out void* activationFactoryPtr);

        // If the operation succeeded, wrap the activation factory into a managed reference
        activationFactory = WellKnownErrorCodes.Succeeded(hresult)
            ? WindowsRuntimeObjectReference.AttachUnsafe(ref activationFactoryPtr, in WellKnownInterfaceIIDs.IID_IActivationFactory)
            : null;

        return hresult;
    }

    /// <summary>
    /// Tries to activate a Windows Runtime type from the current module.
    /// </summary>
    /// <param name="runtimeClassName">The runtime class name for the type to activate.</param>
    /// <param name="activationFactory">The resulting activation factory instance.</param>
    /// <returns>The <c>HRESULT</c> for the operation.</returns>
    public HRESULT GetActivationFactoryUnsafe(string runtimeClassName, out void* activationFactory)
    {
        // Use a fast-pass 'HSTRING' and invoke the export to get the activation factory for the class
        fixed (char* runtimeClassIdPtr = runtimeClassName)
        fixed (void** activationFactoryPtr = &activationFactory)
        {
            HStringMarshaller.ConvertToUnmanagedUnsafe(runtimeClassIdPtr, runtimeClassName.Length, out HStringReference reference);

            return _dllGetActivationFactory(reference.HString, activationFactoryPtr);
        }
    }

    /// <remarks>This method doesn't use <see cref="LoadedModuleCache"/>.</remarks>
    /// <inheritdoc cref="TryLoad"/>
    private static bool TryCreate(string fileName, [NotNullWhen(true)] out WindowsRuntimeDllModule? module)
    {
        HANDLE moduleHandle;

        // Ported from um/libloaderapi.h in the Windows SDK for Windows 10.0.26100.0
        const int LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008;

        // Explicitly look for module in the same directory as this one, and use altered
        // search path to ensure that any dependencies in the same directory are found.
        moduleHandle = WindowsRuntimeImports.LoadLibraryExW(
            lpLibFileNameUtf16: Path.Combine(ApplicationBaseDirectory, fileName),
            hFile: (HANDLE)null,
            dwFlags: LOAD_WITH_ALTERED_SEARCH_PATH);

        // If we failed to load manually, defer to 'NativeLibrary' as a fallback
        if (moduleHandle == (HANDLE)null)
        {
            _ = NativeLibrary.TryLoad(fileName, typeof(WindowsRuntimeDllModule).Assembly, null, out moduleHandle);
        }

        // Nothing else we can do, there's no .dll with this name that we can find
        if (moduleHandle == (HANDLE)null)
        {
            module = null;

            return false;
        }

        void* dllGetActivationFactory = WindowsRuntimeImports.TryGetProcAddress(moduleHandle, "DllGetActivationFactory"u8);

        // If we can't find the 'DllGetActivationFactory' export, the .dll is invalid (this export must be present)
        if (dllGetActivationFactory == null)
        {
            module = null;

            return false;
        }

        // We consider the 'DllCanUnloadNow' export to be optional, to make this more flexible (not all .dll-s export this one)
        void* dllCanUnloadNow = WindowsRuntimeImports.TryGetProcAddress(moduleHandle, "DllCanUnloadNow"u8);

        module = new WindowsRuntimeDllModule(
            fileName,
            moduleHandle,
            (delegate* unmanaged[Stdcall]<HSTRING, void**, HRESULT>)dllGetActivationFactory,
            (delegate* unmanaged[Stdcall]<HRESULT>)dllCanUnloadNow);

        return true;
    }
}

