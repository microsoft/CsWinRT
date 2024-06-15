#include "pch.h"

#if _M_IX86
#pragma comment(lib, "libs/CustomNativeMain-x86.lib")
#elif _M_X64
#pragma comment(lib, "libs/CustomNativeMain-x64.lib")
#elif _M_ARM
#pragma comment(lib, "libs/CustomNativeMain-arm.lib")
#elif _M_ARM64
#pragma comment(lib, "libs/CustomNativeMain-arm64.lib")
#else
#error Invalid CPU architecture (only x86, x64, arm and arm64 are supported)
#endif

extern "C" int __managed__Main(int argc, wchar_t* argv[]);

int __cdecl wmain(int argc, wchar_t* argv[])
{
    return __managed__Main(argc, argv);
}