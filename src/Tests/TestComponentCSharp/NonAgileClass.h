#pragma once
#include "NonAgileClass.g.h"

namespace winrt::TestComponentCSharp::implementation
{
    struct NonAgileClass : NonAgileClassT<NonAgileClass, winrt::non_agile>
    {
        winrt::event<Windows::Foundation::EventHandler<winrt::Windows::Foundation::IInspectable>> _event;
    public:
        NonAgileClass();
        void Observe(Microsoft::UI::Xaml::Interop::IBindableObservableVector vector);
        void VectorChanged(Microsoft::UI::Xaml::Interop::IBindableObservableVector vector, Windows::Foundation::IInspectable e);

        winrt::event_token CanExecuteChanged(winrt::Windows::Foundation::EventHandler<winrt::Windows::Foundation::IInspectable> const& handler);
        void CanExecuteChanged(winrt::event_token const& token) noexcept;
        bool CanExecute(winrt::Windows::Foundation::IInspectable const& parameter);
        void Execute(winrt::Windows::Foundation::IInspectable const& parameter);

        // Overriding runtime class name to allow marshaling as just the ICommand interface during proxy calls
        // to avoid defining proxy stubs for this dll.
        hstring GetRuntimeClassName() const
        {
            return L"Windows.UI.Xaml.Input.ICommand";
        }
    };
}
namespace winrt::TestComponentCSharp::factory_implementation
{
    struct NonAgileClass : NonAgileClassT<NonAgileClass, implementation::NonAgileClass, Windows::Foundation::IStringable>
    {
        hstring ToString()
        {
            return L"NonAgileClass";
        }
    };
}
