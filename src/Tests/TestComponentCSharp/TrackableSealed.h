#pragma once
#include "TrackableSealed.g.h"
#include "TrackableSealed.g.h"

extern void* CreateTrackerObject(_In_opt_ IUnknown* outer, _Outptr_ IUnknown** inner);

namespace winrt::TestComponentCSharp::implementation
{
    struct TrackableSealed : TrackableSealedT<TrackableSealed>
    {
        TrackableSealed() = default;

        winrt::event<Windows::Foundation::TypedEventHandler<TestComponentCSharp::TrackableSealed, hstring>> _stringChanged;
        winrt::event_token StringPropertyChanged(Windows::Foundation::TypedEventHandler<TestComponentCSharp::TrackableSealed, hstring> const& handler);
        void StringPropertyChanged(winrt::event_token const& token) noexcept;
    };
}
namespace winrt::TestComponentCSharp::factory_implementation
{
    struct TrackableSealed : TrackableSealedT<TrackableSealed, implementation::TrackableSealed>
    {
        auto ActivateInstance() const
        {
            auto obj = make<implementation::TrackableSealed>();
            IUnknown* inner;
            auto ret = CreateTrackerObject((IUnknown*)winrt::detach_abi(obj), &inner);
            return ret;
        }
    };
}
