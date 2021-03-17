#pragma once
#include "TrackableComposable.g.h"

extern void* CreateTrackerObject(_In_opt_ IUnknown* outer, _Outptr_ IUnknown** inner);

namespace winrt::TestComponentCSharp::implementation
{
    struct TrackableComposable : TrackableComposableT<TrackableComposable>
    {
        TrackableComposable() = default;

        winrt::event<Windows::Foundation::TypedEventHandler<TestComponentCSharp::TrackableComposable, hstring>> _stringChanged;
        winrt::event_token StringPropertyChanged(Windows::Foundation::TypedEventHandler<TestComponentCSharp::TrackableComposable, hstring> const& handler);
        void StringPropertyChanged(winrt::event_token const& token) noexcept;
    };
}
namespace winrt::TestComponentCSharp::factory_implementation
{
    struct TrackableComposable : TrackableComposableT<TrackableComposable, implementation::TrackableComposable>
    {
        auto ActivateInstance() const
        {
            auto obj = make<implementation::TrackableComposable>();
            IUnknown* inner;
            auto ret = CreateTrackerObject((IUnknown*)winrt::detach_abi(obj), &inner);
            return ret;
        }
    };
}
