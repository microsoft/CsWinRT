#pragma once
#include "NonAgileClass.g.h"

namespace winrt::TestComponentCSharp::implementation
{
    struct NonAgileClass : NonAgileClassT<NonAgileClass, winrt::non_agile>
    {
    public:
        NonAgileClass();
        void Observe(Microsoft::UI::Xaml::Interop::IBindableObservableVector vector);
        void VectorChanged(Microsoft::UI::Xaml::Interop::IBindableObservableVector vector, Windows::Foundation::IInspectable e);
    };
}
namespace winrt::TestComponentCSharp::factory_implementation
{
    struct NonAgileClass : NonAgileClassT<NonAgileClass, implementation::NonAgileClass>
    {
    };
}
