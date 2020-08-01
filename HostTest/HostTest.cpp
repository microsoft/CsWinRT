#include "pch.h"
#include "io.h"
#include <filesystem>
#include "CppUnitTest.h"
#include <winrt/TestComponent.h>
#include <winrt/TestComponentCSharp.h>

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

namespace HostTest
{
	struct Manifest
	{
		std::FILE* _fptr;
		std::wstring _path;

		Manifest(const std::wstring& text) 
		{
			if (tmpfile_s(&_fptr) != 0)
			{
				winrt::throw_hresult(E_FAIL);
			}
			std::fputws(text.c_str(), _fptr);
			_path.resize(MAX_PATH);
			auto handle = (HANDLE)_get_osfhandle(_fileno(_fptr));
			if (GetFinalPathNameByHandleW(handle, _path.data(), _path.size(), 0) == 0)
			{
				winrt::throw_last_error();
			}
			//_path = std::to_string(fileno(_handle));

			//char tmpName[MAX_PATH];
			//if (std::tmpnam(tmpName) != nullptr)
			//{
			//	std::file
			//}
			//std::filesystem::temp_directory_path() + std::tmpnam();
		}

		~Manifest()
		{
			if (_fptr != nullptr)
			{
				std::fclose(_fptr);
			}
		}

		operator const std::wstring&()
		{
			return _path;
		}
	};

	struct ActivationContext
	{
		HANDLE _handle = INVALID_HANDLE_VALUE;
		ULONG_PTR _cookie = 0;

		ActivationContext(const std::wstring& manifest) 
		{
			//ACTCTX context = 
			//{
			//	sizeof(ACTCTX),
			//	0,
			//	//(ACTCTX_FLAG_APPLICATION_NAME_VALID | ACTCTX_FLAG_RESOURCE_NAME_VALID),
			//	manifest,
			//	0,
			//	0,
			//	nullptr,
			//	0, //MAKEINTRESOURCE(1),
			//	nullptr,
			//	nullptr
			//};
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

	TEST_CLASS(HostTest)
	{
	public:
		
		// start with explicit manifest with activatable class entry,
		// and activation context pointing to that manifest
		TEST_METHOD(TestMethod1)
		{
			Manifest manifest(LR"(
<?xml version="1.0" encoding="utf-8"?>
<assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1">  
<assemblyIdentity version="1.0.0.0" name="CsWinRT.HostTest"/>
  <file name="WinRTComponent.dll">
    <activatableClass
        name="WinRTComponent.Class1"
        threadingModel="both"
        xmlns="urn:schemas-microsoft-com:winrt.v1" />
  </file>
</assembly>
)");

		
			ActivationContext context(L"HostTest.dll.manifest");
			
			//winrt::TestComponentCSharp::Class object;

			//winrt::TestComponent::TestRunner testRunner;

			// activate TestComponent.TestRunner via host & test projection
			// activate C# runtime class via test projection
			// activate windows type via test projection

			// create projection for windows, & test winmds
			// create activation context to override/control activation behavior
			// activate several classes - mock to confirm?

			// activate TestComponent.Class
			// activate Windows.Foundation.Uri
			// host-based naming convention
			// class-based naming convention
			// target-based naming convention
			// error conditions

		//	add GetActivationFactory to each projection : test, windows, winui
		//		Create Winrt.Host.Test unit test with project reference to winrt.host
		//		copyand rename winrt.host.dll to TestComponentCSharp.Class.Host.dll
		//		use it for host - based activation
		//		also use it class - based for TestComponent.dll
		//		loads winrt.host.dlland
		//		test cases(see spec)
		//		simple :
		//		no config.json, no manifest
		//		errors : test diagnosability
		//		class not found
		//		target not found
		//		.net runtime not found
		//		conflicting fxr(multiple configs)

		}
	};
}
