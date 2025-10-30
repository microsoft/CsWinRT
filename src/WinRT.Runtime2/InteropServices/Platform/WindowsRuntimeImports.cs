// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Native imports for fundamental COM and Windows Runtime APIs.
/// </summary>
internal static unsafe partial class WindowsRuntimeImports
{
    /// <see href="https://learn.microsoft.com/windows/win32/api/combaseapi/nf-combaseapi-cocreateinstance"/>
    [LibraryImport("api-ms-win-core-com-l1-1-0.dll")]
    public static partial HRESULT CoCreateInstance(Guid* rclsid, void* pUnkOuter, uint dwClsContext, Guid* riid, void** ppv);

    /// <see href="https://learn.microsoft.com/windows/win32/api/combaseapi/nf-combaseapi-coincrementmtausage"/>
    [LibraryImport("api-ms-win-core-com-l1-1-0.dll")]
    public static unsafe partial HRESULT CoIncrementMTAUsage(CO_MTA_USAGE_COOKIE* cookie);

    /// <see href="https://learn.microsoft.com/windows/win32/api/combaseapi/nf-combaseapi-codecrementmtausage"/>
    [LibraryImport("api-ms-win-core-com-l1-1-0.dll")]
    public static partial HRESULT CoDecrementMTAUsage(CO_MTA_USAGE_COOKIE cookie);

    /// <see href="https://learn.microsoft.com/windows/win32/api/roapi/nf-roapi-rogetactivationfactory"/>
    [LibraryImport("api-ms-win-core-winrt-l1-1-0.dll")]
    [SupportedOSPlatform("windows6.2")]
    public static partial HRESULT RoGetActivationFactory(HSTRING activatableClassId, Guid* iid, void** factory);

    /// <see href="https://learn.microsoft.com/windows/win32/api/winstring/nf-winstring-windowscreatestring"/>
    [LibraryImport("api-ms-win-core-winrt-string-l1-1-0.dll")]
    [SupportedOSPlatform("windows6.2")]
    public static partial HRESULT WindowsCreateString(char* sourceString, uint length, HSTRING* @string);

    /// <see href="https://learn.microsoft.com/windows/win32/api/winstring/nf-winstring-windowscreatestringreference"/>
    [LibraryImport("api-ms-win-core-winrt-string-l1-1-0.dll")]
    [SupportedOSPlatform("windows6.2")]
    public static partial HRESULT WindowsCreateStringReference(char* sourceString, uint length, HSTRING_HEADER* hstringHeader, HSTRING* @string);

    /// <see href="https://learn.microsoft.com/windows/win32/api/winstring/nf-winstring-windowsdeletestring"/>
    [LibraryImport("api-ms-win-core-winrt-string-l1-1-0.dll")]
    [SupportedOSPlatform("windows6.2")]
    public static partial HRESULT WindowsDeleteString(HSTRING @string);

    /// <see href="https://learn.microsoft.com/windows/win32/api/winstring/nf-winstring-windowsgetstringrawbuffer"/>
    [LibraryImport("api-ms-win-core-winrt-string-l1-1-0.dll")]
    [SupportedOSPlatform("windows6.2")]
    public static partial char* WindowsGetStringRawBuffer(HSTRING @string, uint* length);

    /// <see href="https://learn.microsoft.com/windows/win32/api/combaseapi/nf-combaseapi-rogetagilereference"/>
    [LibraryImport("api-ms-win-core-com-l1-1-1.dll")]
    [SupportedOSPlatform("windows6.3")]
    public static partial HRESULT RoGetAgileReference(AgileReferenceOptions options, Guid* riid, void* pUnk, void** ppAgileReference);

    /// <see href="https://learn.microsoft.com/windows/win32/api/combaseapi/nf-combaseapi-cogetcontexttoken"/>
    [LibraryImport("api-ms-win-core-com-l1-1-0.dll")]
    public static partial HRESULT CoGetContextToken(nuint* pToken);

    /// <see href="https://learn.microsoft.com/windows/win32/api/combaseapi/nf-combaseapi-cogetobjectcontext"/>
    [LibraryImport("api-ms-win-core-com-l1-1-0.dll")]
    public static partial HRESULT CoGetObjectContext(Guid* riid, void** ppv);

    /// <see href="https://learn.microsoft.com/windows/win32/api/oleauto/nf-oleauto-seterrorinfo"/>
    [LibraryImport("oleaut32.dll")]
    public static partial HRESULT SetErrorInfo(uint dwReserved, void* perrinfo);

    /// <see href="https://learn.microsoft.com/windows/win32/api/combaseapi/nf-combaseapi-cocreatefreethreadedmarshaler"/>
    [LibraryImport("api-ms-win-core-com-l1-1-0.dll")]
    public static partial HRESULT CoCreateFreeThreadedMarshaler(void* punkOuter, void** ppunkMarshal);

    /// <see href="https://learn.microsoft.com/windows/win32/api/libloaderapi/nf-libloaderapi-loadlibraryexw"/>
    [LibraryImport("kernel32", SetLastError = true)]
    public static partial HMODULE LoadLibraryExW(char* lpLibFileName, HANDLE hFile, uint dwFlags);

    /// <see href="https://learn.microsoft.com/windows/win32/api/libloaderapi/nf-libloaderapi-getprocaddress"/>
    [LibraryImport("kernel32", SetLastError = true)]
    public static partial void* GetProcAddress(HMODULE hModule, sbyte* lpProcName);

    /// <see href="https://learn.microsoft.com/windows/win32/api/libloaderapi/nf-libloaderapi-freelibrary"/>
    [LibraryImport("kernel32", SetLastError = true)]
    public static partial int FreeLibrary(HMODULE hLibModule);

    /// <see href="https://learn.microsoft.com/windows/win32/api/roerrorapi/nf-roerrorapi-roreportunhandlederror"/>
    [LibraryImport("api-ms-win-core-winrt-error-l1-1-1.dll")]
    public static partial HRESULT RoReportUnhandledError(void* pRestrictedErrorInfo);

    /// https://learn.microsoft.com/windows/win32/api/roerrorapi/nf-roerrorapi-rooriginatelanguageexception
    [LibraryImport("api-ms-win-core-winrt-error-l1-1-1.dll")]
    public static partial int RoOriginateLanguageException(HRESULT error, HSTRING message, void* languageException);

    /// <see href="https://learn.microsoft.com/windows/win32/api/roerrorapi/nf-roerrorapi-getrestrictederrorinfo"/>
    [LibraryImport("api-ms-win-core-winrt-error-l1-1-1.dll")]
    public static partial HRESULT GetRestrictedErrorInfo(void** ppRestrictedErrorInfo);

    /// <see href="https://learn.microsoft.com/windows/win32/api/roerrorapi/nf-roerrorapi-setrestrictederrorinfo"/>
    [LibraryImport("api-ms-win-core-winrt-error-l1-1-1.dll")]
    public static partial HRESULT SetRestrictedErrorInfo(void* pRestrictedErrorInfo);

    /// <see href="https://learn.microsoft.com/windows/win32/api/winbase/nf-winbase-formatmessagew"/>
    [LibraryImport("kernel32.dll")]
    public static partial uint FormatMessageW(uint dwFlags, void* lpSource, uint dwMessageId, uint dwLanguageId, char** lpBuffer, uint nSize, void* pArguments);

    /// <see href="https://learn.microsoft.com/windows/win32/api/winbase/nf-winbase-localfree"/>
    [LibraryImport("kernel32.dll")]
    public static partial void* LocalFree(void* hMem);
}
