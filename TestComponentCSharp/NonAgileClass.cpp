#include "pch.h"
#include "NonAgileClass.h"
#include "NonAgileClass.g.cpp"

using namespace winrt;

namespace
{
    struct __declspec(uuid("624cd4e1-d007-43b1-9c03-af4d3e6258c4")) __declspec(novtable)
        INonAgileBindableVectorChangedEventHandler : ::IUnknown
    {
        virtual int32_t __stdcall Invoke(void*, void*) noexcept = 0;
    };

    struct NonAgileDelegate : implements<NonAgileDelegate, non_agile, INonAgileBindableVectorChangedEventHandler, IMarshal>
    {
        NonAgileDelegate()
        {
        }
        
        int32_t __stdcall Invoke(void* p1, void* p2) noexcept override
        {
            VectorChanged(*reinterpret_cast<Microsoft::UI::Xaml::Interop::IBindableObservableVector const*>(&p1),
                          *reinterpret_cast<Windows::Foundation::IInspectable const*>(&p2));
            return S_OK;
        }

        HRESULT __stdcall GetUnmarshalClass(REFIID riid, void* pv, DWORD dwDestContext, void* pvDestContext, DWORD mshlflags, CLSID* pCid) noexcept final
        {
            if (m_marshaler)
            {
                return m_marshaler->GetUnmarshalClass(riid, pv, dwDestContext, pvDestContext, mshlflags, pCid);
            }

            return E_OUTOFMEMORY;
        }

        HRESULT __stdcall GetMarshalSizeMax(REFIID riid, void* pv, DWORD dwDestContext, void* pvDestContext, DWORD mshlflags, DWORD* pSize) noexcept final
        {
            if (m_marshaler)
            {
                return m_marshaler->GetMarshalSizeMax(riid, pv, dwDestContext, pvDestContext, mshlflags, pSize);
            }

            return E_OUTOFMEMORY;
        }

        HRESULT __stdcall MarshalInterface(IStream* pStm, REFIID riid, void* pv, DWORD dwDestContext, void* pvDestContext, DWORD mshlflags) noexcept final
        {
            if (m_marshaler)
            {
                return m_marshaler->MarshalInterface(pStm, riid, pv, dwDestContext, pvDestContext, mshlflags);
            }

            return E_OUTOFMEMORY;
        }

        HRESULT __stdcall UnmarshalInterface(IStream* pStm, REFIID riid, void** ppv) noexcept final
        {
            if (m_marshaler)
            {
                return m_marshaler->UnmarshalInterface(pStm, riid, ppv);
            }

            *ppv = nullptr;
            return E_OUTOFMEMORY;
        }

        HRESULT __stdcall ReleaseMarshalData(IStream* pStm) noexcept final
        {
            if (m_marshaler)
            {
                return m_marshaler->ReleaseMarshalData(pStm);
            }

            return E_OUTOFMEMORY;
        }

        HRESULT __stdcall DisconnectObject(DWORD dwReserved) noexcept final
        {
            if (m_marshaler)
            {
                return m_marshaler->DisconnectObject(dwReserved);
            }

            return E_OUTOFMEMORY;
        }

        void VectorChanged(Microsoft::UI::Xaml::Interop::IBindableObservableVector vector, Windows::Foundation::IInspectable e)
        {
            int32_t sum = 0;
            auto view = vector.GetView();
            for (uint32_t i = 0; i < view.Size(); i++)
            {
                sum += winrt::unbox_value<int32_t>(view.GetAt(i));
            }
            e.as<winrt::TestComponentCSharp::IProperties2>().ReadWriteProperty(sum);
        }

    private:

        static com_ptr<::IMarshal> get_marshaler() noexcept
        {
            com_ptr<::IUnknown> unknown;
            WINRT_VERIFY_(S_OK, CoCreateFreeThreadedMarshaler(nullptr, unknown.put()));
            return unknown ? unknown.try_as<::IMarshal>() : nullptr;
        }

        com_ptr<::IMarshal> m_marshaler{ get_marshaler() };
    };
}

namespace winrt::TestComponentCSharp::implementation
{
    NonAgileClass::NonAgileClass()
    {
    }

    void NonAgileClass::Observe(Microsoft::UI::Xaml::Interop::IBindableObservableVector vector)
    {
        Microsoft::UI::Xaml::Interop::BindableVectorChangedEventHandler handler;
        *put_abi(handler) = make<NonAgileDelegate>().detach();               
        vector.VectorChanged(handler);
    }
}
