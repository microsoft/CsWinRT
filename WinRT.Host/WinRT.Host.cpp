// WinRT.Host.cpp : Implementation of C#/WinRT managed runtime component host

#include "pch.h"
#include <filesystem>

EXTERN_C IMAGE_DOS_HEADER __ImageBase;

#include <unknwn.h>
#include <winrt/Windows.Foundation.h>
#include <roerrorapi.h>

// NetHost headers, found via NetHostDir.csproj:GetNetHostDir
#if __has_include("nethost.h")
#include <nethost.h>
#else
#error Project build required to resolve NetHost directory.  Individual compilation not supported.
#endif
#include <coreclr_delegates.h>
#include <hostfxr.h>

#include <mscoree.h>

// These error and exit codes are document in the host-error-codes.md
enum StatusCode
{
    // Success
    Success = 0,
    Success_HostAlreadyInitialized = 0x00000001,
    Success_DifferentRuntimeProperties = 0x00000002,

    // Failure
    InvalidArgFailure = 0x80008081,
    CoreHostLibLoadFailure = 0x80008082,
    CoreHostLibMissingFailure = 0x80008083,
    CoreHostEntryPointFailure = 0x80008084,
    CoreHostCurHostFindFailure = 0x80008085,
    // unused                           = 0x80008086,
    CoreClrResolveFailure = 0x80008087,
    CoreClrBindFailure = 0x80008088,
    CoreClrInitFailure = 0x80008089,
    CoreClrExeFailure = 0x8000808a,
    ResolverInitFailure = 0x8000808b,
    ResolverResolveFailure = 0x8000808c,
    LibHostCurExeFindFailure = 0x8000808d,
    LibHostInitFailure = 0x8000808e,
    // unused                           = 0x8000808f,
    LibHostExecModeFailure = 0x80008090,
    LibHostSdkFindFailure = 0x80008091,
    LibHostInvalidArgs = 0x80008092,
    InvalidConfigFile = 0x80008093,
    AppArgNotRunnable = 0x80008094,
    AppHostExeNotBoundFailure = 0x80008095,
    FrameworkMissingFailure = 0x80008096,
    HostApiFailed = 0x80008097,
    HostApiBufferTooSmall = 0x80008098,
    LibHostUnknownCommand = 0x80008099,
    LibHostAppRootFindFailure = 0x8000809a,
    SdkResolverResolveFailure = 0x8000809b,
    FrameworkCompatFailure = 0x8000809c,
    FrameworkCompatRetry = 0x8000809d,
    // unused                           = 0x8000809e,
    BundleExtractionFailure = 0x8000809f,
    BundleExtractionIOError = 0x800080a0,
    LibHostDuplicateProperty = 0x800080a1,
    HostApiUnsupportedVersion = 0x800080a2,
    HostInvalidState = 0x800080a3,
    HostPropertyNotFound = 0x800080a4,
    CoreHostIncompatibleConfig = 0x800080a5,
    HostApiUnsupportedScenario = 0x800080a6,
};


namespace
{
    // Globals to hold hostfxr exports
    hostfxr_initialize_for_runtime_config_fn hostfxr_initialize_for_runtime_config;
    hostfxr_get_runtime_delegate_fn hostfxr_get_runtime_delegate;
    hostfxr_close_fn hostfxr_close;
    hostfxr_set_error_writer_fn hostfxr_set_error_writer;

    // Using the nethost library, discover the location of hostfxr and get exports
    bool load_hostfxr()
    {
        static const auto is_hostfxr_loaded = [&]()
        {
            return (hostfxr_initialize_for_runtime_config && hostfxr_get_runtime_delegate &&
                hostfxr_close && hostfxr_set_error_writer);
        };

        if (is_hostfxr_loaded())
        {
            return true;
        }

        // Pre-allocate a large buffer for the path to hostfxr
        wchar_t buffer[MAX_PATH];
        size_t buffer_size = sizeof(buffer) / sizeof(wchar_t);
        int rc = get_hostfxr_path(buffer, &buffer_size, nullptr);
        if (rc != 0)
            return false;

        // Load hostfxr and get desired exports
        auto lib = ::LoadLibraryW(buffer);
        if (lib == 0)
        {
            return false;
        }
        hostfxr_initialize_for_runtime_config = (hostfxr_initialize_for_runtime_config_fn)::GetProcAddress(lib, "hostfxr_initialize_for_runtime_config");
        hostfxr_get_runtime_delegate = (hostfxr_get_runtime_delegate_fn)::GetProcAddress(lib, "hostfxr_get_runtime_delegate");
        hostfxr_close = (hostfxr_close_fn)::GetProcAddress(lib, "hostfxr_close");
        hostfxr_set_error_writer = (hostfxr_set_error_writer_fn)::GetProcAddress(lib, "hostfxr_set_error_writer");

        return is_hostfxr_loaded();
    }

    static load_assembly_and_get_function_pointer_fn load_assembly_and_get_function_pointer = nullptr;

    // Load and initialize .NET Core and get desired function pointer for scenario
    HRESULT get_dotnet_load_assembly(const wchar_t* host_path, const wchar_t* target_config)
    {
        hostfxr_handle context = nullptr;

        hostfxr_initialize_parameters parameters
        {
            sizeof(hostfxr_initialize_parameters),
            host_path,
            nullptr//dotnet_root.c_str()
        };

        HRESULT hr = hostfxr_initialize_for_runtime_config(target_config, /*&parameters*/nullptr, &context);
        if (hr == Success_HostAlreadyInitialized || hr == Success_DifferentRuntimeProperties)
        {
            hr = Success;
        }
        else if (hr != Success || context == nullptr)
        {
        }
        else
        {
            // Get the load assembly function pointer
            hr = hostfxr_get_runtime_delegate(
                context,
                hdt_load_assembly_and_get_function_pointer,
                (void**)&load_assembly_and_get_function_pointer);
        }

        if (hr == 0 && load_assembly_and_get_function_pointer == nullptr)
        {
            hr = E_FAIL;
            //std::cerr << "Get delegate failed: " << std::hex << std::showbase << rc << std::endl;
        }

        hostfxr_close(context);
        return hr;
    }
}

extern "C" HRESULT STDMETHODCALLTYPE DllGetActivationFactory(void* hstr_class_id, void** activation_factory)
{
    //winrt::hstring class_id(hstr_class_id, winrt::take_ownership_from_abi_t{});
    // Assumes the managed assembly to load and its runtime configuration file are next to the host
    HRESULT hr;
    wchar_t buffer[MAX_PATH];
    auto size = ::GetModuleFileName((HINSTANCE)&__ImageBase, buffer, _countof(buffer));
    std::filesystem::path host_module(buffer);
    auto host_file = host_module.filename();
    auto host_path = host_module;
    host_path.remove_filename();

    // Load HostFxr and get exported hosting functions
    if (!load_hostfxr())
    {
        //assert(false && "Failure: load_hostfxr()");
        return E_FAIL;
    }
    auto error_writer = [](const wchar_t* message)
    {
        // todo: fix this - diagnostics - RoOriginateError 
        OutputDebugString(message);
    };
    hostfxr_set_error_writer(error_writer);
    //propagate_error_writer_t propagate_error_writer_to_hostfxr(set_error_writer_fn);

    // Probe for target assembly by:
    // - runtime class name
    // - host name (if renamed)
    // - runtimeconfig.json entry? (vs. forwarders)
    winrt::hstring class_id_hstr(hstr_class_id, winrt::take_ownership_from_abi_t{});
    auto target_path = host_path.wstring() + std::wstring(class_id_hstr.c_str());
    winrt::detach_abi(class_id_hstr);

    std::vector<std::wstring> probe_paths;

    // Probe for target assembly by runtime class name
    {
        auto probe = [&](const wchar_t* suffix)
        {
            auto probe_path = target_path + suffix;
            if (std::filesystem::exists(probe_path))
            {
                target_path = probe_path;
                return true;
            }
            probe_paths.emplace_back(std::move(probe_path));
            return false;
        };

        while(true)
        {
            if (probe(L".Server.dll") || probe(L".dll"))
            {
                break;
            }
            std::size_t count = target_path.rfind('.'); 
            if(count == std::wstring::npos)
            {
                target_path.clear();
                break;
            }
            target_path.resize(count);
        }
    }

    // Probe for target assembly by host name, if renamed
    if (target_path.empty() && host_file.wstring() != L"winrt.host.dll")
    {
        probe_paths.push_back(host_module);
        
        target_path = host_module;
        target_path.resize(target_path.size() - 4);

        auto probe = [&](const wchar_t* suffix)
        {
            auto probe_path = target_path + suffix;
            auto end = probe_paths.end();
            if (std::find(probe_paths.begin(), end, probe_path) == end)
            {
                if (std::filesystem::exists(probe_path))
                {
                    target_path = probe_path;
                    return true;
                }
                probe_paths.emplace_back(std::move(probe_path));
            }
            return false;
        };

        while (true)
        {
            if (probe(L".Server.dll") || probe(L".dll"))
            {
                break;
            }
            std::size_t count = target_path.rfind('.');
            if (count == std::wstring::npos)
            {
                target_path.clear();
                break;
            }
            target_path.resize(count);
        }
    }

    if (target_path.empty())
    {
        // now what?
    }

    // Probe for target assembly's runtimeconfig.json, default to winrt.host.runtimeconfig.json 
    std::filesystem::path target_config = target_path;
    target_config.replace_extension(L".runtimeconfig.json");
    if (!std::filesystem::exists(target_config))
    {
        if (host_file.wstring() == L"winrt.host.dll")
        {
            target_config.clear();
        }
        else
        {
            target_config.replace_filename(L"winrt.host.runtimeconfig.json");
            if (!std::filesystem::exists(target_config))
            {
                target_config.clear();
            }
        }
    }
    if (target_config.empty())
    {
        // now what?
    }

    hr = get_dotnet_load_assembly(host_module.wstring().c_str(), target_config.c_str());
    if (hr != ERROR_SUCCESS)
    {
        return hr;
    }
    //assert(load_assembly_and_get_function_pointer != nullptr && "Failure: get_dotnet_load_assembly()");

    // load managed assembly bootstrapper from resource, to avoid needing to know 
    // fully qualified assembly display name of target


    // Load managed assembly and get function pointer to a managed method
    auto target_file = std::filesystem::path(target_path).replace_extension().filename();
    auto target_type = L"WinRT.Module, " + target_file.wstring(); // NOTE: assembly is case-sensitive!
    target_type = L"WinRT.Module, WinRT.Host.Shim";
    //target_type = L"WinRT.Module";
    //auto delegate_type_name = L"System.Func`2[[System.IntPtr],[System.IntPtr]]";
    auto delegate_type_name = L"WinRT.Module+GetActivationFactoryDelegate, " + target_file.wstring();
    delegate_type_name = L"WinRT.Module+GetActivationFactoryDelegate, WinRT.Host.Shim";
    //delegate_type_name = L"WinRT.Module+GetActivationFactoryDelegate";
    typedef int (CORECLR_DELEGATE_CALLTYPE* get_activation_factory_fn)(void* hstr_target, void* hstr_class_id, void** activation_factory);
    get_activation_factory_fn get_activation_factory = nullptr;
    hr = load_assembly_and_get_function_pointer(
        target_path.c_str(),
        target_type.c_str(),
        L"GetActivationFactory",
        delegate_type_name.c_str(),     // TODO: UNMANAGEDCALLERSONLY_METHOD 
        nullptr,
        (void**)&get_activation_factory);
    if (hr != ERROR_SUCCESS)
    {
        return hr;
    }
    //assert(rc == 0 && custom != nullptr && "Failure: load_assembly_and_get_function_pointer()");

    winrt::hstring hstr_target(target_path.c_str());
    return get_activation_factory(winrt::get_abi(hstr_target), hstr_class_id, activation_factory);
}


extern "C" HRESULT STDMETHODCALLTYPE DllCanUnloadNow(void)
{
    return S_FALSE;
}


#if false
#include "redirected_error_writer.h"
#include "hostfxr.h"
#include "fxr_resolver.h"
#include "pal.h"
#include "trace.h"
#include "error_codes.h"
#include "utils.h"
#include <hstring.h>

#if defined(_WIN32)

// WinRT entry points are defined without the __declspec(dllexport) attribute.
// The issue here is that the compiler will throw an error regarding linkage
// redefinion. The solution here is to the use a .def file on Windows.
#define WINRT_API extern "C"

#else

#define WINRT_API SHARED_API

#endif // _WIN32

using winrt_activation_fn = pal::hresult_t(STDMETHODCALLTYPE*)(const pal::wchar_t* appPath, HSTRING activatableClassId, IActivationFactory** factory);

namespace
{
    int get_winrt_activation_delegate(pal::std::wstring* app_path, winrt_activation_fn* delegate)
    {
        return load_fxr_and_get_delegate(
            hostfxr_delegate_type::hdt_winrt_activation,
            [app_path](const pal::std::wstring& host_path, pal::std::wstring* target_config_out)
        {
            // Change the extension to get the 'app' and config
            size_t idx = host_path.rfind(_X(".dll"));
            assert(idx != pal::std::wstring::npos);

            pal::std::wstring app_path_local{ host_path };
            app_path_local.replace(app_path_local.begin() + idx, app_path_local.end(), _X(".winmd"));
            *app_path = std::move(app_path_local);

            pal::std::wstring target_config_local{ host_path };
            target_config_local.replace(target_config_local.begin() + idx, target_config_local.end(), _X(".runtimeconfig.json"));
            *target_config_out = std::move(target_config_local);

            return StatusCode::Success;
        },
            delegate
            );
    }
}


WINRT_API HRESULT STDMETHODCALLTYPE DllGetActivationFactory(_In_ HSTRING activatableClassId, _Out_ IActivationFactory** factory)
{
    HRESULT hr;
    pal::std::wstring app_path;
    winrt_activation_fn activator;
    {
        trace::setup();
        reset_redirected_error_writer();
        error_writer_scope_t writer_scope(redirected_error_writer);

        int ec = get_winrt_activation_delegate(&app_path, &activator);
        if (ec != StatusCode::Success)
        {
            RoOriginateErrorW(__HRESULT_FROM_WIN32(ec), 0 /* message is null-terminated */, get_redirected_error_string().c_str());
            return __HRESULT_FROM_WIN32(ec);
        }
    }

    return activator(app_path.c_str(), activatableClassId, factory);
}
#endif

