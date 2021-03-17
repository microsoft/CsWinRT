#include "pch.h"
#include "TrackableComposable.h"
#include "TrackableComposable.g.cpp"

namespace winrt::TestComponentCSharp::implementation
{
    winrt::event_token TrackableComposable::StringPropertyChanged(Windows::Foundation::TypedEventHandler<TestComponentCSharp::TrackableComposable, hstring> const& handler)
    {
        return _stringChanged.add(handler);
    }
    void TrackableComposable::StringPropertyChanged(winrt::event_token const& token) noexcept
    {
        _stringChanged.remove(token);
    }
}
