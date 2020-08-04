#pragma once

#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#undef GetCurrentTime

#define WINRT_LEAN_AND_MEAN
#include "winrt/Windows.Foundation.h"

#pragma push_macro("X86")
#pragma push_macro("X64")
#undef X86
#undef X64
#include "winrt/Windows.System.h"
#pragma pop_macro("X64")
#pragma pop_macro("X86")

#include "gtest/gtest.h"
