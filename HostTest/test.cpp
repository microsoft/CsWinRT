#include "pch.h"
#include "io.h"
#include <winrt/Windows.Foundation.h>
#include <winrt/TestComponent.h>
#include <winrt/TestComponentCSharp.h>

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

// Resolve target assembly (Test.dll) when it has been renamed to
// TestComponentCSharp.Server.dll.  Note that this technique requires
// the WinRT.Host.Shim.dll to load the target assembly and find the type.
// HostFxr would otherwise require a fully qualified type name, which can't
// be known when the target assembly display name and file name differ.
TEST(HostTest, ProbeByClass)
{
	ActivationContext context(L"ProbeByClass.manifest");
	winrt::TestComponentCSharp::ManagedClass object;
	auto string = object.ToString();
	EXPECT_TRUE(string == L"ManagedClass");
}

// Resolve target assembly (Test.dll) when the host has been renamed
// from WinRT.Host.dll to Test.Host.dll.
TEST(HostTest, ProbeByHost)
{
	ActivationContext context(L"ProbeByHost.manifest");
	winrt::TestComponentCSharp::ManagedClass object;
	auto string = object.ToString();
	EXPECT_TRUE(string == L"ManagedClass");
}

// Find ACID->Target mapping in winrt.host.runtimeconfig.json ?
TEST(HostTest, MappedTarget)
{
}

TEST(HostTest, ClassNotFound)
{
}

TEST(HostTest, TargetNotFound)
{
}

TEST(HostTest, RuntimeNotFound)
{
}

TEST(HostTest, RuntimeConfigConflict)
{
}

TEST(HostTest, RuntimeConfigNotFound)
{
}
