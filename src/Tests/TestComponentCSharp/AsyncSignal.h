// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma once

#include <windows.h>
#include <winrt/base.h>
#include <coroutine>

namespace winrt::TestComponentCSharp::implementation
{
    // The fixture signals the event explicitly on cancellation. Avoid the lifetime race in
    // C++/WinRT's cancellation-aware waiter: https://github.com/microsoft/cppwinrt/issues/1329.
    struct async_signal_awaiter
    {
        explicit async_signal_awaiter(HANDLE signal) noexcept : m_signal(signal)
        {
        }

        bool await_ready() const
        {
            DWORD result = ::WaitForSingleObject(m_signal, 0);
            if (result == WAIT_FAILED)
            {
                winrt::throw_last_error();
            }

            return result == WAIT_OBJECT_0;
        }

        void await_suspend(std::coroutine_handle<> resume)
        {
            m_resume = resume;
            m_wait.attach(winrt::check_pointer(::CreateThreadpoolWait(callback, this, nullptr)));

            // Publishing the callback must be the last access to this awaiter: it can
            // resume the coroutine and destroy the awaiter before this call returns.
            ::SetThreadpoolWait(m_wait.get(), m_signal, nullptr);
        }

        void await_resume() const noexcept
        {
        }

    private:
        static void CALLBACK callback(PTP_CALLBACK_INSTANCE, void* context, PTP_WAIT, TP_WAIT_RESULT) noexcept
        {
            auto resume = static_cast<async_signal_awaiter*>(context)->m_resume;
            resume();
        }

        struct wait_traits
        {
            using type = PTP_WAIT;

            static void close(type value) noexcept
            {
                ::CloseThreadpoolWait(value);
            }

            static constexpr type invalid() noexcept
            {
                return nullptr;
            }
        };

        HANDLE m_signal;
        winrt::handle_type<wait_traits> m_wait;
        std::coroutine_handle<> m_resume;
    };
}
