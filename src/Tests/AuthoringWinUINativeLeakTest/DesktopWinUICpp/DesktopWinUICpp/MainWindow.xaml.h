#pragma once

#pragma push_macro("GetCurrentTime")
#undef GetCurrentTime

#include "MainWindow.g.h"

#pragma pop_macro("GetCurrentTime")

namespace winrt::DesktopWinUICpp::implementation
{
    struct MainWindow : MainWindowT<MainWindow>
    {
        MainWindow();

        WinUIComponent::CustomButton customButton;
        winrt::weak_ref<WinUIComponent::CustomButton> customButtonWeak;
        Microsoft::UI::Xaml::Controls::Button regularButton;
        winrt::weak_ref<Microsoft::UI::Xaml::Controls::Button> regularButtonWeak;

        void ReleaseRegularButton();
        void ReleaseCustomButton();

        void acquireRegularButton_Click(Windows::Foundation::IInspectable const& sender, Microsoft::UI::Xaml::RoutedEventArgs const& args);
        void acquireCustomButton_Click(Windows::Foundation::IInspectable const& sender, Microsoft::UI::Xaml::RoutedEventArgs const& args);
        void releaseRegularButton_Click(Windows::Foundation::IInspectable const& sender, Microsoft::UI::Xaml::RoutedEventArgs const& args);
        void releaseCustomButton_Click(Windows::Foundation::IInspectable const& sender, Microsoft::UI::Xaml::RoutedEventArgs const& args);

        void isAliveRegularButton_Click(Windows::Foundation::IInspectable const& sender, Microsoft::UI::Xaml::RoutedEventArgs const& args);
        void isAliveCustomButton_Click(Windows::Foundation::IInspectable const& sender, Microsoft::UI::Xaml::RoutedEventArgs const& args);
    };
}

namespace winrt::DesktopWinUICpp::factory_implementation
{
    struct MainWindow : MainWindowT<MainWindow, implementation::MainWindow>
    {
    };
}
