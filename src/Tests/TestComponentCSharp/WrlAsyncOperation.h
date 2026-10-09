#pragma once

#include <functional>
#include <winrt/Windows.Foundation.h>

namespace winrt::TestComponentCSharp::implementation
{
    Windows::Foundation::IAsyncOperationWithProgress<uint32_t, uint32_t> CreateWrlAsyncAddition(
        uint32_t lhs, uint32_t rhs, std::function<bool(int32_t)>& complete,
        Windows::Foundation::AsyncOperationWithProgressCompletedHandler<uint32_t, uint32_t> const& observer = nullptr);
}
