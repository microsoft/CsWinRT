#pragma once

#if defined(_DEBUG)
// Statically linking to nethost requires matching CRT
#undef _DEBUG
#endif

#define WIN32_LEAN_AND_MEAN             
#include <windows.h>
