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
        const wchar_t* message;
    }
    hostfxr_status_messages[] =
    {
        { InvalidArgFailure, L"One of the specified arguments for the operation is invalid." },
        { CoreHostLibLoadFailure, L"There was a failure loading a dependent library." },
        { CoreHostLibMissingFailure, L"One of the dependent libraries is missing." },
        { CoreHostEntryPointFailure, L"One of the dependent libraries is missing a required entry point." },
        { CoreHostCurHostFindFailure, L"Could not deduce installation location from current module." },
        { CoreClrResolveFailure, L"The coreclr library could not be found." },
        { CoreClrBindFailure, L"The loaded coreclr library doesn't have one of the required entry points." },
        { CoreClrInitFailure, L"The call to coreclr_initialize failed." },
        { CoreClrExeFailure, L"The call to coreclr_execute_assembly failed." },
        { ResolverInitFailure, L"Initialization of the hostpolicy dependency resolver failed." },
        { ResolverResolveFailure, L"Resolution of dependencies in hostpolicy failed." },
        { LibHostCurExeFindFailure, L"Failure to determine the location of the current executable." },
        { LibHostInitFailure, L"Initialization of the hostpolicy library failed." },
        { LibHostSdkFindFailure, L"Failure to find the requested SDK. This happens in the hostfxr when an SDK(also called CLI) command is used with dotnet. In this case the hosting layer tries to find an installed. NET SDK to run the command on. The search is based on deduced install locationand on the requested version from potential global. json file. If either no matching SDK version can be found, or that version exists, but it's missing the dotnet. dll file, this error code is returned." },
        { LibHostInvalidArgs, L"Arguments to hostpolicy are invalid." },
        { InvalidConfigFile, L"The .runtimeconfig.json file is invalid." },
        { AppArgNotRunnable, L"The command line for dotnet.exe doesn't contain the path to the application." },
        { AppHostExeNotBoundFailure, L"Apphost failed to determine which application to run." },
        { FrameworkMissingFailure, L"It was not possible to find a compatible framework version." },
        { HostApiFailed, L"The hostpolicy could not calculate the NATIVE_DLL_SEARCH_DIRECTORIES." },
        { HostApiBufferTooSmall, L"The buffer specified to an API is not big enough to fit the requested value." },
        { LibHostUnknownCommand, L"The corehost_main_with_output_buffer is called with unsupported host command." },
        { LibHostAppRootFindFailure, L"The imprinted application path doesn't exist." },
        { SdkResolverResolveFailure, L"hostfxr_resolve_sdk2 failed to find matching SDK." },
        { FrameworkCompatFailure, L"The .runtimeconfig.json contains two incompatible framework references." },
        { FrameworkCompatRetry, L"A previously processed framework reference was reprocessed." },
        { BundleExtractionFailure, L"Error extracting single-file apphost bundle." },
        { BundleExtractionIOError, L"Error reading or writing files during single-file apphost bundle extraction." },
        { LibHostDuplicateProperty, L"The .runtimeconfig.json contains a runtime property which is also produced by the hosting layer." },
        { HostApiUnsupportedVersion, L"Feature which requires certain version of the hosting layer binaries was used on a version which doesn't support it." },
        { HostInvalidState, L"The current state is incompatible with the requested operation." },
        { HostPropertyNotFound, L"A property requested by hostfxr_get_runtime_property_value doesn't exist." },
        { CoreHostIncompatibleConfig, L"The component being initialized requires framework which is not available" },
        { HostApiUnsupportedScenario, L"hostfxr doesn't currently support requesting the given delegate type using the given context." },
    };

    for (auto&& elem : hostfxr_status_messages)
    {
        if (elem.status == result)
        {
            throw hresult_error(result, elem.message);
        }
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

    hostfxr_initialize_for_runtime_config = (hostfxr_initialize_for_runtime_config_fn)::GetProcAddress(lib, "hostfxr_initialize_for_runtime_config");
    hostfxr_get_runtime_delegate = (hostfxr_get_runtime_delegate_fn)::GetProcAddress(lib, "hostfxr_get_runtime_delegate");
    hostfxr_close = (hostfxr_close_fn)::GetProcAddress(lib, "hostfxr_close");
    hostfxr_set_error_writer = (hostfxr_set_error_writer_fn)::GetProcAddress(lib, "hostfxr_set_error_writer");

    if (!is_hostfxr_loaded())
    {
        winrt::throw_last_error();
    }
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
        check_hostfxr_hresult(load_assembly_and_get_function_pointer(
            L"WinRT.Host.Shim.dll",
            L"WinRT.Host.Shim, WinRT.Host.Shim",
            L"GetActivationFactory",
            // TODO: UNMANAGEDCALLERSONLY_METHOD 
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
