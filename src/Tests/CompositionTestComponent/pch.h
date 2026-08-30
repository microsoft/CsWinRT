#pragma once

#include <unknwn.h>

// Undefine GetCurrentTime macro to prevent conflicts with Windows Runtime types
#undef GetCurrentTime

#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Foundation.Collections.h>
#include <winrt/AuthoringTest.h>
