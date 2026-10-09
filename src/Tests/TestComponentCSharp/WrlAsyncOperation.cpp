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

    class WrlAsyncAddition final : public Microsoft::WRL::RuntimeClass<
        Microsoft::WRL::RuntimeClassFlags<Microsoft::WRL::WinRt>, WrlAsyncOperation, WrlAsyncBase>
    {
        InspectableClass(L"TestComponentCSharp.WrlAsyncAddition", BaseTrust);

    public:
        HRESULT RuntimeClassInitialize(uint32_t result)
        {
            _result = result;
            return Start();
        }

        STDMETHOD(put_Completed)(WrlCompletedHandler* handler) override
        {
            return PutOnComplete(handler);
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

        HRESULT Complete(HRESULT hr)
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
    };
}

namespace winrt::TestComponentCSharp::implementation
{
    Windows::Foundation::IAsyncOperationWithProgress<uint32_t, uint32_t> CreateWrlAsyncAddition(
        uint32_t lhs, uint32_t rhs, std::function<void(int32_t)>& complete)
    {
        ::Microsoft::WRL::ComPtr<WrlAsyncAddition> operation;
        winrt::check_hresult(::Microsoft::WRL::MakeAndInitialize<WrlAsyncAddition>(&operation, lhs + rhs));
        complete = [operation](int32_t hr)
        {
            winrt::check_hresult(operation->Complete(hr));
        };

        ::Microsoft::WRL::ComPtr<WrlAsyncOperation> abi;
        winrt::check_hresult(operation.As(&abi));
        return { abi.Detach(), winrt::take_ownership_from_abi };
    }
}
