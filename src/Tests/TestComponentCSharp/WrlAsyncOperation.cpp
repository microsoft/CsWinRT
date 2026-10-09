#include "pch.h"
#include "WrlAsyncOperation.h"
#include <windows.storage.streams.h>
#include <wrl/async.h>

namespace
{
    // Use the SDK's UInt32/UInt32 ABI specialization, also used by stream write operations.
    using WrlAsyncOperation = ABI::Windows::Foundation::IAsyncOperationWithProgress<UINT32, UINT32>;
    using WrlCompletedHandler = ABI::Windows::Foundation::IAsyncOperationWithProgressCompletedHandler<UINT32, UINT32>;
    using WrlProgressHandler = ABI::Windows::Foundation::IAsyncOperationProgressHandler<UINT32, UINT32>;
    using WrlAsyncBase = Microsoft::WRL::AsyncBase<WrlCompletedHandler, WrlProgressHandler,
        Microsoft::WRL::SingleResult, Microsoft::WRL::DisableCausality>;

    struct __declspec(uuid("98819937-5DA6-4AD6-8E46-2B34A7C1718F")) IWrlAsyncCompletion : IUnknown
    {
        virtual HRESULT STDMETHODCALLTYPE Complete(HRESULT hr) = 0;
    };

    class WrlAsyncAddition final : public Microsoft::WRL::RuntimeClass<
        Microsoft::WRL::RuntimeClassFlags<Microsoft::WRL::WinRtClassicComMix>,
        WrlAsyncOperation, WrlAsyncBase, IWrlAsyncCompletion>
    {
        InspectableClass(L"TestComponentCSharp.WrlAsyncAddition", BaseTrust);

    public:
        HRESULT RuntimeClassInitialize(uint32_t result, WrlCompletedHandler* observer)
        {
            _result = result;
            _observer = observer;
            return Start();
        }

        STDMETHOD(put_Completed)(WrlCompletedHandler* handler) override
        {
            if (!_observer || !handler)
            {
                return PutOnComplete(handler);
            }

            auto observer = _observer;
            Microsoft::WRL::ComPtr<WrlCompletedHandler> completed = handler;
            auto observed = Microsoft::WRL::Callback<WrlCompletedHandler>(
                [observer, completed](WrlAsyncOperation* operation, ABI::Windows::Foundation::AsyncStatus status)
                {
                    HRESULT hr = observer->Invoke(operation, status);
                    return FAILED(hr) ? hr : completed->Invoke(operation, status);
                });
            if (!observed)
            {
                return E_OUTOFMEMORY;
            }
            return PutOnComplete(observed.Get());
        }

        STDMETHOD(get_Completed)(WrlCompletedHandler** handler) override
        {
            return GetOnComplete(handler);
        }

        STDMETHOD(put_Progress)(WrlProgressHandler* handler) override
        {
            return PutOnProgress(handler);
        }

        STDMETHOD(get_Progress)(WrlProgressHandler** handler) override
        {
            return GetOnProgress(handler);
        }

        STDMETHOD(GetResults)(UINT32* result) override
        {
            *result = 0;
            HRESULT hr = CheckValidStateForResultsCall();
            if (SUCCEEDED(hr))
            {
                *result = _result;
            }
            return hr;
        }

        STDMETHOD(Cancel)() override
        {
            HRESULT hr = WrlAsyncBase::Cancel();
            return FAILED(hr) ? hr : FireCompletion();
        }

        STDMETHOD(Complete)(HRESULT hr) override
        {
            if (FAILED(hr))
            {
                TryTransitionToError(hr);
            }
            return FireCompletion();
        }

    private:
        HRESULT OnStart() override { return S_OK; }
        void OnClose() override {}
        void OnCancel() override {}

        uint32_t _result = 0;
        Microsoft::WRL::ComPtr<WrlCompletedHandler> _observer;
    };
}

namespace winrt::TestComponentCSharp::implementation
{
    Windows::Foundation::IAsyncOperationWithProgress<uint32_t, uint32_t> CreateWrlAsyncAddition(
        uint32_t lhs, uint32_t rhs, std::function<bool(int32_t)>& complete,
        Windows::Foundation::AsyncOperationWithProgressCompletedHandler<uint32_t, uint32_t> const& observer)
    {
        ::Microsoft::WRL::ComPtr<WrlAsyncAddition> operation;
        winrt::check_hresult(::Microsoft::WRL::MakeAndInitialize<WrlAsyncAddition>(
            &operation, lhs + rhs, reinterpret_cast<WrlCompletedHandler*>(winrt::get_abi(observer))));

        // The test's completion controller must not keep the native operation or its handler alive.
        ::Microsoft::WRL::WeakRef weak;
        winrt::check_hresult(operation.AsWeak(&weak));
        complete = [weak](int32_t hr)
        {
            ::Microsoft::WRL::ComPtr<WrlAsyncOperation> operation;
            winrt::check_hresult(weak.As(&operation));
            if (!operation)
            {
                return false;
            }
            ::Microsoft::WRL::ComPtr<IWrlAsyncCompletion> target;
            winrt::check_hresult(operation.As(&target));
            winrt::check_hresult(target->Complete(hr));
            return true;
        };

        ::Microsoft::WRL::ComPtr<WrlAsyncOperation> abi;
        winrt::check_hresult(operation.As(&abi));
        return { abi.Detach(), winrt::take_ownership_from_abi };
    }
}
