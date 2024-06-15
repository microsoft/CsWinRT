#include "pch.h"
#include <windows.h>

#pragma comment(lib, "shell32.lib")

typedef int (*__managed__Main)(int, wchar_t*[]);

int APIENTRY wWinMain(
    _In_ HINSTANCE hInstance,
    _In_opt_ HINSTANCE hPrevInstance,
    _In_ LPWSTR    lpCmdLine,
    _In_ int       nCmdShow)
{
    wchar_t fileName[MAX_PATH];

    // Get the path of the current .exe file (it will be renamed to match the app name)
    if (!GetModuleFileNameW(NULL, fileName, MAX_PATH))
    {
        return GetLastError();
    }

    int fileNameLength = lstrlenW(fileName);

    // Replace the extension (.exe -> .dll)
    memcpy(
        /* _Dst */ &fileName[fileNameLength - 3 /* "exe" */],
        /* _Src */ L"dll",
        /* _Size */ sizeof(wchar_t) * 3 /* "dll" */);

    // Load the .dll for the app
    HMODULE hModule = LoadLibraryW(fileName);

    if (!hModule)
    {
        return GetLastError();
    }

    // Get the custom main from the native .dll (hardcoded to "__managed__Main")
    FARPROC pEntryPoint = GetProcAddress(hModule, "__managed__Main");

    if (!pEntryPoint)
    {
        return GetLastError();
    }

    // We need to parse the arguments to get back 'argc', 'argv' for the managed entry point
    int argc;
    LPWSTR* argv = CommandLineToArgvW(lpCmdLine, &argc);

    // Jump to the custom entry point in the implementation .dll
    return ((__managed__Main)pEntryPoint)(argc, argv);
}