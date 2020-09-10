#include "pch.h"
#include "Class.h"
#include "ComImports.h"
#include "ComImports.g.cpp"

using namespace winrt::Windows::Foundation;

namespace winrt::TestComponentCSharp::implementation
{
    namespace statics
    {
        int _count{};
    }

    struct __declspec(uuid("EECDBF0E-BAE9-4CB6-A68E-9598E1CB57BB")) IWindowNative : ::IUnknown
    {
        virtual HRESULT __stdcall get_WindowHandle(HWND* hWnd) noexcept = 0;
    };

    struct __declspec(uuid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1")) IInitializeWithWindow : ::IUnknown
    {
        virtual HRESULT __stdcall Initialize(HWND hWnd) noexcept = 0;
    };

    struct ComImportObject : winrt::implements<ComImportObject, IInspectable, IWindowNative, IInitializeWithWindow>
    {
        ComImportObject()
        {
            ++statics::_count;
        }

        ~ComImportObject()
        {
            --statics::_count;
        }

        HWND m_hWnd = (HWND)0;
        
        HRESULT __stdcall get_WindowHandle(HWND* hWnd) noexcept override
        {
            *hWnd = m_hWnd;
            return 0;
        }

        HRESULT __stdcall Initialize(HWND hWnd) noexcept override
        {
            m_hWnd = hWnd;
            return 0;
        }
    };

    IInspectable ComImports::MakeObject()
    {
        return winrt::make<ComImportObject>();
    }

    int32_t ComImports::NumObjects()
    {
        return statics::_count;
    }
}
