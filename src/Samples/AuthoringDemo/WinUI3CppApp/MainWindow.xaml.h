#pragma once

#include "MainWindow.g.h"

namespace winrt::WinUI3CppApp::implementation
{
    struct MainWindow : MainWindowT<MainWindow>
    {
        MainWindow();

        void sayHelloButton_Click(Windows::Foundation::IInspectable const& sender, Microsoft::UI::Xaml::RoutedEventArgs const& args);
    };
}

namespace winrt::WinUI3CppApp::factory_implementation
{
    struct MainWindow : MainWindowT<MainWindow, implementation::MainWindow>
    {
    };
}
