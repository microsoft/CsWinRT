// This unit test exercises WinRT.Host.dll, which provides hosting for
// runtime components written in C#.  For details on the behavior of 
// the host, see the spec.
// 
// Each test case specifies:
// 1. A runtime class to be activated: 
//		ProbeByClass, ProbeByHost for expected successes
//		ClassNotFound for expected failures
// 2. An activation context manifest, with activatableClass entry
//		to specify the WinRT host dll, which may be renamed.
// 3. A runtimeconfig.json corresponding to the host dll above,
//		to configure the CLR and optionally provide an activatableClasses
//		entry to specify an explicit mapping to the target assembly.

#include "pch.h"
#include "io.h"
#include <filesystem>
#include <winrt/Windows.Foundation.h>
#include <winrt/TestHost.h>
#include "../../Authoring/WinRT.Host/hostfxr_status.h"

using namespace winrt::TestHost;

EXTERN_C IMAGE_DOS_HEADER __ImageBase;

struct ActivationContext
{
	HANDLE _handle = INVALID_HANDLE_VALUE;
	ULONG_PTR _cookie = 0;

	ActivationContext(const std::wstring& manifest)
	{
		wchar_t buffer[MAX_PATH];
		auto size = ::GetModuleFileName((HINSTANCE)&__ImageBase, buffer, _countof(buffer));
		std::filesystem::path manifest_path(buffer);
		manifest_path.replace_filename(manifest);
		ACTCTX context = { sizeof(ACTCTX), 0, manifest_path.c_str() };
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

template<typename TClass>
winrt::hstring Activate(const wchar_t* manifest, winrt::hresult expected_error = 0)
{
	try
	{
		ActivationContext context(manifest);
		TClass object;
		auto string = object.ToString();
		EXPECT_EQ(expected_error, 0);
		return string;
	}
	catch (winrt::hresult_error hr)
	{
		EXPECT_EQ(expected_error, hr.code());
		return {};
	}
}

// Note: this test must precede all others to ensure no CLR is already loaded
TEST(HostTest, RuntimeNotFound)
{
	Activate<ClassNotFound>(L"RuntimeNotFound.manifest", FrameworkMissingFailure);
}

// ClassId:				Host:				Target:
// TestHost.Class		WinRT.Host.dll		TestHost.ProbeByClass.dll
// 
// Resolve to target assembly name based on runtime class ID.
// Note that this technique requires WinRT.Host.Shim.dll to load the target assembly 
// and find the type.  HostFxr would otherwise require a fully qualified type name, 
// which can't be known when the target assembly display name and file name differ.
TEST(HostTest, ProbeByClass)
{
	EXPECT_TRUE(Activate<ProbeByClass>(L"ProbeByClass.manifest") == L"TestHost.ProbeByClass.dll");
}

// ClassId:				Host:				Target:
// TestHost.Class		Test.Host.dll		Test.dll
// 
// Resolve to target assembly name based on renamed host dll.
TEST(HostTest, ProbeByHost)
{
	EXPECT_TRUE(Activate<ProbeByHost>(L"ProbeByHost.manifest") == L"Test.dll");
}

// ClassId:				Host:					Target:
// TestHost.Class		MappedTarget.Host.dll	TestHost.ProbeByClass.dll
// 
// Resolve to target assembly name based on runtimeconfig.json mapping,
// when both Host and Target dll names are fixed (e.g., manifest-free).
TEST(HostTest, MappedTarget)
{
	EXPECT_TRUE(Activate<ProbeByClass>(L"MappedTarget.manifest") == L"TestHost.ProbeByClass.dll");
}

// No target assembly found via probing or mapping
TEST(HostTest, TargetNotFound)
{
	Activate<ClassNotFound>(L"TargetNotFound.manifest", HRESULT_FROM_WIN32(ERROR_MOD_NOT_FOUND));
}

// Mapped target assembly that does not implement the given runtime class
TEST(HostTest, ClassNotFound)
{
	Activate<ClassNotFound>(L"ClassNotFound.manifest", CLASS_E_CLASSNOTAVAILABLE);
}

// .runtimeconfig.json activatableClass entry with invalid target assembly
TEST(HostTest, BadMappedTarget)
{
	Activate<ClassNotFound>(L"BadMappedTarget.manifest", InvalidConfigFile);
}

// Renamed host dll with no .runtimeconfig.json 
TEST(HostTest, RuntimeConfigNotFound)
{
	Activate<ClassNotFound>(L"NoRuntimeConfig.manifest", InvalidConfigFile);
}

// Fail if attempting to load a conflicting runtime
TEST(HostTest, RuntimeConflict)
{
	Activate<ClassNotFound>(L"RuntimeNotFound.manifest", CoreHostIncompatibleConfig);
}

