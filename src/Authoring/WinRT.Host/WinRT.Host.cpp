// WinRT.Host.cpp : Implementation of C#/WinRT managed runtime component host

#include "pch.h"
#include "hostfxr_status.h"
#include <filesystem>
#include <sstream>

#undef GetObject  

#include <unknwn.h>
#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Data.Json.h>
#include <winrt/Windows.Storage.h>
#include <roerrorapi.h>

// provides CString, used to retrieve error messages from Resource string table
#include "atlstr.h"

using namespace winrt;
using namespace winrt::Windows::Storage;
using namespace winrt::Windows::Data::Json;

// NetHost headers, found via NetHostDir.csproj:GetNetHostDir
#if __has_include("nethost.h")
#include <nethost.h>
#else
#error Project build required to resolve NetHost directory.  Individual compilation not supported.
#endif
#include <coreclr_delegates.h>
#include <hostfxr.h>

#include <mscoree.h>

// Global function pointers
typedef int (CORECLR_DELEGATE_CALLTYPE* get_activation_factory_fn)(
    void* hstr_target_path, void* hstr_class_id, void** activation_factory);
static get_activation_factory_fn get_activation_factory = nullptr;
static hostfxr_close_fn hostfxr_close = nullptr;
static hostfxr_get_runtime_delegate_fn hostfxr_get_runtime_delegate = nullptr;
static hostfxr_initialize_for_runtime_config_fn hostfxr_initialize_for_runtime_config = nullptr;
static hostfxr_set_error_writer_fn hostfxr_set_error_writer = nullptr;
static load_assembly_and_get_function_pointer_fn load_assembly_and_get_function_pointer = nullptr;

[[noreturn]] inline __declspec(noinline) void throw_hostfxr_hresult(hresult const result)
{
    static const struct
    {
        HostFxrStatus status;
        int resourceID;
    }
    hostfxr_status_messages[] =
    {
        { InvalidArgFailure, 106 },
        { CoreHostLibLoadFailure, 107 },
        { CoreHostLibMissingFailure, 108 },
		{ CoreHostEntryPointFailure, 109 },
        { CoreHostCurHostFindFailure, 110 },
        { CoreClrResolveFailure, 111 },
        { CoreClrBindFailure, 112 },
        { CoreClrInitFailure, 113 },
        { CoreClrExeFailure, 114 },
        { ResolverInitFailure, 115 },
        { ResolverResolveFailure, 116 },
        { LibHostCurExeFindFailure, 117 },
        { LibHostInitFailure, 118 },
        { LibHostSdkFindFailure, 119 },
        { LibHostInvalidArgs, 120 },
        { InvalidConfigFile, 121 },
        { AppArgNotRunnable, 122 },
        { AppHostExeNotBoundFailure, 123 },
        { FrameworkMissingFailure, 124 },
        { HostApiFailed, 125 },
        { HostApiBufferTooSmall, 126 },
        { LibHostUnknownCommand, 127 },
        { LibHostAppRootFindFailure, 128 },
        { SdkResolverResolveFailure, 129 },
        { FrameworkCompatFailure, 130 },
        { FrameworkCompatRetry, 131 },
        { BundleExtractionFailure, 132 },
        { BundleExtractionIOError, 133 },
        { LibHostDuplicateProperty, 134 },
        { HostApiUnsupportedVersion, 135 },
        { HostInvalidState, 136 },
        { HostPropertyNotFound, 137 },
        { CoreHostIncompatibleConfig, 138 },
        { HostApiUnsupportedScenario, 139 },
    };

    auto string_table_entry = 0;
    for (auto&& elem : hostfxr_status_messages)
    {   
        if (elem.status == result)
        {
            string_table_entry = elem.resourceID;
        }
    }

    if (string_table_entry != 0)
    { 
        // The longest string in the table is 460 characters, we make a buffer of 461 to allow enough space for any case.
        TCHAR buf[461];
        HMODULE resources_handle;
        resources_handle = LoadLibrary(L"WinRT.Host.ErrorStrings.dll");
        if (resources_handle)
        {
            LoadString(resources_handle, string_table_entry, buf, sizeof(buf));
            // Done
            FreeLibrary(resources_handle);
        }
        throw hresult_error(result, buf);
    }
    
    winrt::throw_hresult(result);
}

inline void check_hostfxr_hresult(hresult const result)
{
    if (result < 0)
    {
        throw_hostfxr_hresult(result);
    }
}

// Using the nethost library, discover the location of hostfxr and get exports
void load_hostfxr()
{
    static const auto is_hostfxr_loaded = [&]()
    {
        return(hostfxr_initialize_for_runtime_config &&
            hostfxr_get_runtime_delegate &&
            hostfxr_close &&
            hostfxr_set_error_writer);
    };

    if (is_hostfxr_loaded())
    {
        return;
    }

    wchar_t buffer[MAX_PATH];
    size_t buffer_size = sizeof(buffer) / sizeof(wchar_t);
    check_hostfxr_hresult(get_hostfxr_path(buffer, &buffer_size, nullptr));
    auto lib = ::LoadLibraryW(buffer);
    if (lib == 0)
    {
        winrt::throw_last_error();
    }

    if ((hostfxr_initialize_for_runtime_config = (hostfxr_initialize_for_runtime_config_fn)::GetProcAddress(lib, "hostfxr_initialize_for_runtime_config")) &&
        (hostfxr_get_runtime_delegate = (hostfxr_get_runtime_delegate_fn)::GetProcAddress(lib, "hostfxr_get_runtime_delegate")) &&
        (hostfxr_close = (hostfxr_close_fn)::GetProcAddress(lib, "hostfxr_close")) &&
        (hostfxr_set_error_writer = (hostfxr_set_error_writer_fn)::GetProcAddress(lib, "hostfxr_set_error_writer")))
    {
        return;
    }

    winrt::throw_last_error();
}

struct error_writer
{
    static thread_local std::wstringstream _message;
    hostfxr_error_writer_fn _previous_writer;

    error_writer()
    {
        _previous_writer = hostfxr_set_error_writer([](const wchar_t* message)
        {
            _message << message << L"\r\n";
        });
    }

    ~error_writer()
    {
        hostfxr_set_error_writer(_previous_writer);
    }
};
thread_local std::wstringstream error_writer::_message;

// Load and initialize .NET runtime and get assembly load function pointer
void init_runtime(const wchar_t* host_path, const wchar_t* host_config)
{
    struct hostfxr_context
    {
        ~hostfxr_context()
        {
            if (_handle != nullptr)
            {
                hostfxr_close(_handle);
            }
        }
        hostfxr_handle _handle = nullptr;
    };
    hostfxr_context context;

    error_writer writer;
    HRESULT hr = hostfxr_initialize_for_runtime_config(host_config, nullptr, &context._handle);
    if (hr == Success_HostAlreadyInitialized || hr == Success_DifferentRuntimeProperties)
    {
        hr = Success;
    }
    else if (hr != Success)
    {
        auto message = writer._message.str();
        throw hresult_error(hr, message);
    }
    
    if(load_assembly_and_get_function_pointer == nullptr)
    {
        // Get the load assembly function pointer
        hr = hostfxr_get_runtime_delegate(
            context._handle,
            hdt_load_assembly_and_get_function_pointer,
            (void**)&load_assembly_and_get_function_pointer);
        check_hostfxr_hresult(hr);
    }
}

std::wstring find_mapped_target_assembly(std::filesystem::path host_config, winrt::hstring class_id)
{
    std::wstring target_assembly;

    try
    {
        auto config_file = StorageFile::GetFileFromPathAsync(host_config.c_str()).get();
        auto json_string = FileIO::ReadTextAsync(config_file).get();
        JsonObject root_object;
        if (JsonObject::TryParse(json_string, root_object))
        {
            if (auto classes = root_object.TryLookup(L"activatableClasses"); classes)
            {
                if (auto value_type = classes.ValueType(); value_type == JsonValueType::Object)
                {
                    if (auto class_path = classes.GetObject().TryLookup(class_id); class_path)
                    {
                        target_assembly = class_path.GetString().c_str();
                    }
                }
            }
        }
    }
    catch (const winrt::hresult_error&)
    {
    }

    return target_assembly;
}

std::filesystem::path probe_for_target_assembly(std::filesystem::path host_module, winrt::hstring class_id)
{
    auto host_file = host_module.filename();
    auto host_path = host_module;
    host_path.remove_filename();

    std::wstring target_path;

    std::vector<std::wstring> probe_paths;

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

    auto shorten_target_path = [&]()
    {
        std::size_t count = target_path.rfind('.');
        if (count == std::wstring::npos)
        {
            target_path.clear();
            return false;
        }
        target_path.resize(count);
        return true;
    };

    auto probe_target = [&]()
    {
        while (!probe(L".Server.dll") && !probe(L".dll") && shorten_target_path()) {};
        return !target_path.empty();
    };

    // Probe for target assembly by host name, if renamed (most common)
    if (host_file.wstring() != L"winrt.host.dll")
    {
        probe_paths.push_back(host_module);
        target_path = host_module;
        target_path.resize(target_path.size() - 4);
        if (probe_target())
        {
            return target_path;
        }
    }

    // Probe for target assembly by runtime class name (less common)
    target_path = host_path.wstring() + std::wstring(class_id.c_str());
    if(probe_target())
    {
        return target_path;
    }

    return {};
}

EXTERN_C IMAGE_DOS_HEADER __ImageBase;

void GetActivationFactory(void* hstr_class_id, void** activation_factory)
{
    // Assumes the managed assembly to load and its runtime configuration file are next to the host
    wchar_t buffer[MAX_PATH];
    auto size = ::GetModuleFileName((HINSTANCE)&__ImageBase, buffer, _countof(buffer));
    std::filesystem::path host_module(buffer);
    auto host_file = host_module.filename();
    auto host_path = host_module;
    host_path.remove_filename();

    // Load HostFxr and get exported hosting functions
    load_hostfxr();

    // Determine host runtimeconfig.json from module name and load the runtime with it
    winrt::hstring class_id;
    winrt::copy_from_abi(class_id, hstr_class_id);
    std::filesystem::path host_config = host_module;
    host_config.replace_extension(L".runtimeconfig.json");
    std::filesystem::path target_path;
    if (std::filesystem::exists(host_config))
    {
        // If host runtimeconfig.json found, look for a target assembly mapping in it
        auto target_assembly = find_mapped_target_assembly(host_config, class_id);
        if (!target_assembly.empty())
        {
            target_path = host_module;
            target_path.replace_filename(target_assembly);
            if (!std::filesystem::exists(target_path))
            {
                throw_hostfxr_hresult(InvalidConfigFile);
            }
        }
    }
    else
    {
        // TODO: create a reasonable default runtimeconfig.json?
        throw_hostfxr_hresult(InvalidConfigFile);
    }

    init_runtime(host_module.wstring().c_str(), host_config.c_str());

    // If no explicit target assembly mapping found, probe for it by naming convention
    if (target_path.empty())
    {
        target_path = probe_for_target_assembly(host_module, class_id);
        if (target_path.empty() || !std::filesystem::exists(target_path))
        {
            winrt::throw_hresult(HRESULT_FROM_WIN32(ERROR_MOD_NOT_FOUND));
        }
    }

    // Load shim (managed portion of host) and retrieve get_activation_factory pointer
    if (::get_activation_factory == nullptr)
    {
        auto shim_path = host_module;
        shim_path.replace_filename("WinRT.Host.Shim.dll");
        check_hostfxr_hresult(load_assembly_and_get_function_pointer(
            shim_path.wstring().c_str(),
            L"WinRT.Host.Shim, WinRT.Host.Shim",
            L"GetActivationFactory",
            L"WinRT.Host.Shim+GetActivationFactoryDelegate, WinRT.Host.Shim",     
            nullptr,
            (void**)&::get_activation_factory));
    }

    // Load target assembly and get managed runtime class activation factory 
    winrt::hstring hstr_target_path(target_path.c_str());
    check_hostfxr_hresult(::get_activation_factory(
        winrt::get_abi(hstr_target_path), hstr_class_id, activation_factory));
}

extern "C" HRESULT STDMETHODCALLTYPE DllGetActivationFactory(void* hstr_class_id, void** activation_factory)
{
    try
    {
        GetActivationFactory(hstr_class_id, activation_factory);
        return S_OK;
    }
    catch (const winrt::hresult_error& hr)
    {
        return hr.to_abi();
    }
    catch (const std::exception& e)
    {
        std::string message(e.what());
        std::wstring message_wide(message.begin(), message.end());
        winrt::hresult_error hr(winrt::hresult(E_FAIL), winrt::hstring(message_wide));
        return hr.to_abi();
    }
    catch (...)
    {
        return E_FAIL;
    }
}

extern "C" HRESULT STDMETHODCALLTYPE DllCanUnloadNow(void)
{
    return S_FALSE;
}
