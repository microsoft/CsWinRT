#include "pch.h"
#include "io.h"
#include <winrt/Windows.Foundation.h>
#include <winrt/TestComponent.h>
#include <winrt/TestComponentCSharp.h>
#include "../WinRT.Host/hostfxr_status.h"

using namespace winrt::TestComponentCSharp;

struct ActivationContext
{
	HANDLE _handle = INVALID_HANDLE_VALUE;
	ULONG_PTR _cookie = 0;

	ActivationContext(const std::wstring& manifest)
	{
		ACTCTX context = { sizeof(ACTCTX), 0, manifest.c_str() };
		_handle = CreateActCtxW(&context);
		if ((_handle == INVALID_HANDLE_VALUE) || !ActivateActCtx(_handle, &_cookie))
		{
			winrt::throw_last_error();
		}
	}

	~ActivationContext()
	{
		if (_cookie != 0)
		{
			DeactivateActCtx(0, _cookie);
		}
		if (_handle != INVALID_HANDLE_VALUE)
		{
			ReleaseActCtx(_handle);
		}
	}
};

template<typename TClass = ManagedClass>
void ActivateManagedClass(const wchar_t* manifest, winrt::hresult expected_error = 0)
{
	try
	{
		ActivationContext context(manifest);
		TClass object;
		auto string = object.ToString();
		EXPECT_TRUE(string == L"ManagedClass");
		EXPECT_TRUE(expected_error == 0);
	}
	catch (winrt::hresult_error hr)
	{
		EXPECT_TRUE(expected_error == hr.code());
	}
}


// ClassId:								Host:				Target:
// TestComponentCSharp.ManagedClass		WinRT.Host.dll		TestComponentCSharp.ManagedClass.dll
// 
// Resolve to target assembly name based on runtime class ID.
// Note that this technique requires WinRT.Host.Shim.dll to load the target assembly 
// and find the type.  HostFxr would otherwise require a fully qualified type name, 
// which can't be known when the target assembly display name and file name differ.
TEST(Passing, ProbeByClass)
{
	ActivateManagedClass(L"ProbeByClass.manifest");
}

// ClassId:								Host:				Target:
// TestComponentCSharp.ManagedClass		Test.Host.dll		Test.dll
// 
// Resolve to target assembly name based on renamed host dll.
TEST(Passing, ProbeByHost)
{
	ActivateManagedClass(L"ProbeByHost.manifest");
}

// ClassId:								Host:				Target:
// TestComponentCSharp.ManagedClass		MappedTarget.dll	Test.dll
// 
// Resolve to target assembly name based on runtimeconfig.json mapping,
// when both Host and Target dll names are fixed (e.g., manifest-free).
TEST(Passing, MappedTarget)
{
	ActivateManagedClass(L"MappedTarget.manifest");
}

// No target assembly found via mapping or probing
TEST(Failing, TargetNotFound)
{
//	ActivateManagedClass<ManagedClassNotFound>(L"TargetNotFound.manifest", REGDB_E_CLASSNOTREG);
}

// Mapped target assembly that does not implement the given runtime class
TEST(Failing, ClassNotFound)
{
	ActivateManagedClass<ManagedClassNotFound>(L"ClassNotFound.manifest", REGDB_E_CLASSNOTREG);
}

// .runtimeconfig.json activatableClass entry with bad target path
TEST(Failing, RuntimeConfigBadTarget)
{
	ActivateManagedClass<ManagedClassNotFound>(L"BadRuntimeConfig.manifest", InvalidConfigFile);
}

// Renamed host dll with no .runtimeconfig.json 
TEST(Failing, RuntimeConfigNotFound)
{
	ActivateManagedClass<ManagedClassNotFound>(L"NoRuntimeConfig.manifest", InvalidConfigFile);
}

TEST(Failing, RuntimeConfigConflict)
{
}

TEST(Failing, RuntimeNotFound)
{
}

