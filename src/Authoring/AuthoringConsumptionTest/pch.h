#pragma once

// Undefine GetCurrentTime macro to prevent
// conflict with Storyboard::GetCurrentTime
#undef GetCurrentTime

#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Foundation.Collections.h>

#pragma push_macro("X86")
#pragma push_macro("X64")
#undef X86
#undef X64
#include "winrt/Windows.System.h"
#pragma pop_macro("X64")
#pragma pop_macro("X86")

#include <winrt/AuthoringSample.h>

#include "gtest/gtest.h"
