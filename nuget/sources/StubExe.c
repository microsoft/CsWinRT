#include <wchar.h>

// Declare the import for '__managed__Main', which is a special export produced by the
// Native AOT runtime to allow jumping into the real 'Main' of an application. This is
// exported automatically when the 'CustomNativeMain' property is set to 'true'.
__declspec(dllimport)
int __stdcall __managed__Main(int argc, wchar_t** argv);

// Our entry point is simply a direct jump into the entry point from Native AOT
int wmain(int argc, wchar_t** argv)
{
    return __managed__Main(argc, argv);
}