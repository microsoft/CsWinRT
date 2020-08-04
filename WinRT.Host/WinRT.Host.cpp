// WinRT.Host.cpp : Implementation of C#/WinRT managed runtime component host

#include "pch.h"
#include "hostfxr_status.h"
#include <filesystem>

EXTERN_C IMAGE_DOS_HEADER __ImageBase;

#undef GetObject  

#include <unknwn.h>
#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Data.Json.h>
#include <winrt/Windows.Storage.h>
#include <roerrorapi.h>

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

// Global HostFxr function pointers
static hostfxr_initialize_for_runtime_config_fn hostfxr_initialize_for_runtime_config = nullptr;
static hostfxr_get_runtime_delegate_fn hostfxr_get_runtime_delegate = nullptr;
static hostfxr_close_fn hostfxr_close = nullptr;
static hostfxr_set_error_writer_fn hostfxr_set_error_writer = nullptr;
static load_assembly_and_get_function_pointer_fn load_assembly_and_get_function_pointer = nullptr;

// Using the nethost library, discover the location of hostfxr and get exports
void load_hostfxr()
{
    static const auto is_hostfxr_loaded = [&]()
    {
        return( hostfxr_initialize_for_runtime_config && 
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
    winrt::check_hresult(get_hostfxr_path(buffer, &buffer_size, nullptr));
    auto lib = ::LoadLibraryW(buffer);
    if (lib == 0)
    {
        winrt::throw_last_error();
    }

    hostfxr_initialize_for_runtime_config = (hostfxr_initialize_for_runtime_config_fn)::GetProcAddress(lib, "hostfxr_initialize_for_runtime_config");
    hostfxr_get_runtime_delegate = (hostfxr_get_runtime_delegate_fn)::GetProcAddress(lib, "hostfxr_get_runtime_delegate");
    hostfxr_close = (hostfxr_close_fn)::GetProcAddress(lib, "hostfxr_close");
    hostfxr_set_error_writer = (hostfxr_set_error_writer_fn)::GetProcAddress(lib, "hostfxr_set_error_writer");

    if (!is_hostfxr_loaded())
    {
        winrt::throw_last_error();
    }
}

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

    HRESULT hr = hostfxr_initialize_for_runtime_config(host_config, nullptr, &context._handle);
    if (hr == Success_HostAlreadyInitialized || hr == Success_DifferentRuntimeProperties)
    {
        hr = Success;
    }
    winrt::check_hresult(hr);
    
    if(load_assembly_and_get_function_pointer == nullptr)
    {
        // Get the load assembly function pointer
        hr = hostfxr_get_runtime_delegate(
            context._handle,
            hdt_load_assembly_and_get_function_pointer,
            (void**)&load_assembly_and_get_function_pointer);
        winrt::check_hresult(hr);
    }
}

std::wstring find_target_assembly_mapping(std::filesystem::path host_config, winrt::hstring class_id)
{
    std::wstring target_path;

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
                        target_path = class_path.GetString().c_str();
                    }
                }
            }
        }
    }
    catch (const winrt::hresult_error&)
    {
    }

    return target_path;
}

std::wstring probe_for_target_assembly(std::filesystem::path host_module, winrt::hstring class_id)
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

    // Probe for target assembly by runtime class name
    target_path = host_path.wstring() + std::wstring(class_id.c_str());
    if (!probe_target())
    {
        if (host_file.wstring() == L"winrt.host.dll")
        {
            return{};
        }

        // Probe for target assembly by host name, if renamed
        probe_paths.push_back(host_module);
        target_path = host_module;
        target_path.resize(target_path.size() - 4);
        if (!probe_target())
        {
            return{};
        }
    }

    return target_path;
}

void GetActivationFactory(void* hstr_class_id, void** activation_factory)
{
    // Assumes the managed assembly to load and its runtime configuration file are next to the host
    HRESULT hr;
    wchar_t buffer[MAX_PATH];
    auto size = ::GetModuleFileName((HINSTANCE)&__ImageBase, buffer, _countof(buffer));
    std::filesystem::path host_module(buffer);

    // Load HostFxr and get exported hosting functions
    load_hostfxr();
    auto error_writer = [](const wchar_t* message)
    {
        // todo: fix this - diagnostics - RoOriginateError 
        OutputDebugString(message);
    };
    hostfxr_set_error_writer(error_writer);

    winrt::hstring class_id;
    winrt::copy_from_abi(class_id, hstr_class_id);

    // Determine host runtimeconfig.json from module name and load the runtime with it
    std::filesystem::path host_config = host_module;
    host_config.replace_extension(L".runtimeconfig.json");
    std::wstring target_path;
    if (std::filesystem::exists(host_config))
    {
        // If host runtimeconfig.json found, look for a target assembly mapping in it
        target_path = find_target_assembly_mapping(host_config, class_id);
        if (!target_path.empty() && !std::filesystem::exists(target_path))
        {
            winrt::throw_hresult(InvalidConfigFile);
        }
    }
    else
    {
        winrt::throw_hresult(InvalidConfigFile);
        // TODO: create a reasonable default runtimeconfig.json
        // it would be nice if hostfxr_initialize_for_runtime_config made this an optional param
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

    // Load managed assembly and get function pointer to a managed method
    auto target_file = std::filesystem::path(target_path).replace_extension().filename();
    typedef int (CORECLR_DELEGATE_CALLTYPE* get_activation_factory_fn)(
        void* hstr_target_path, void* hstr_class_id, void** activation_factory);
    get_activation_factory_fn get_activation_factory = nullptr;
    winrt::check_hresult(load_assembly_and_get_function_pointer(
        L"WinRT.Host.Shim.dll",
        L"WinRT.Host.Shim, WinRT.Host.Shim",
        L"GetActivationFactory",
        // TODO: UNMANAGEDCALLERSONLY_METHOD 
        L"WinRT.Host.Shim+GetActivationFactoryDelegate, WinRT.Host.Shim",     
        nullptr,
        (void**)&get_activation_factory));

    winrt::hstring hstr_target_path(target_path.c_str());
    winrt::hstring hstr_host_config(host_config.c_str());
    winrt::check_hresult(get_activation_factory(
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
