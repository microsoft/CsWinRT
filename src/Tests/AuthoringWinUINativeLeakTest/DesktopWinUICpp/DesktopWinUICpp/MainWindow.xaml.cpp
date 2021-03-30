#include "pch.h"
#include "MainWindow.xaml.h"
#if __has_include("MainWindow.g.cpp")
#include "MainWindow.g.cpp"
#endif

using namespace winrt;
using namespace Microsoft::UI::Xaml;
using namespace Microsoft::UI::Xaml::Controls;
using namespace WinUIComponent;

namespace winrt::DesktopWinUICpp::implementation
{

    MainWindow::MainWindow()
    {
        InitializeComponent();
    }

    void MainWindow::acquireRegularButton_Click(IInspectable const& sender, RoutedEventArgs const& args)
    {
        regularButton = TestButtons::GetRegularButton();
        regularButtonWeak = winrt::make_weak(regularButton);
    }

    void MainWindow::acquireCustomButton_Click(IInspectable const&, RoutedEventArgs const&)
    {
        customButton = TestButtons::GetCustomButton();
        customButtonWeak = winrt::make_weak(customButton);
    }

    void MainWindow::ReleaseRegularButton()
    {
        regularButton = NULL;
    }

    void MainWindow::ReleaseCustomButton()
    {
        customButton = NULL;
    }

    void MainWindow::releaseRegularButton_Click(IInspectable const& sender, RoutedEventArgs const& args)
    {
        ReleaseRegularButton();
    }

    void MainWindow::releaseCustomButton_Click(IInspectable const&, RoutedEventArgs const&)
    {
        ReleaseCustomButton();
    }

    void MainWindow::setRegularButton_Click(IInspectable const& sender, RoutedEventArgs const& args)
    {
        regularButton = Button();
        TestButtons::SetButton(regularButton);
        regularButtonWeak = winrt::make_weak(regularButton);
    }

    void MainWindow::setCustomButton_Click(IInspectable const&, RoutedEventArgs const&)
    {
        customButton = CustomButton();
        TestButtons::SetCustomButton(customButton);
        customButtonWeak = winrt::make_weak(customButton);
    }

    void MainWindow::releaseManagedRegularButton_Click(IInspectable const& sender, RoutedEventArgs const& args)
    {
        TestButtons::ReleaseRegularButton();
    }

    void MainWindow::releaseManagedCustomButton_Click(IInspectable const&, RoutedEventArgs const&)
    {
        TestButtons::ReleaseCustomButton();
    }

    void MainWindow::isAliveRegularButton_Click(IInspectable const& sender, RoutedEventArgs const& args)
    {
        bool result = TestButtons::IsAliveRegularButton();
        bool result2 = false;
        if (regularButton)
        {
            winrt::get_unknown(regularButton)->AddRef();
            int num = winrt::get_unknown(regularButton)->Release();
            result2 = (num >= 1);
        }
        else if (regularButtonWeak != nullptr)
        {
            result2 = (regularButtonWeak.get() != nullptr);
        }

        labelRegular().Text(L"Regular Button isAlive in C#: " + to_hstring(result) + L", in C++: " + to_hstring(result2));
    }

    void MainWindow::isAliveCustomButton_Click(IInspectable const&, RoutedEventArgs const&)
    {
        bool result = TestButtons::IsAliveCustomButton();
        bool result2 = false;
        if (customButton)
        {
            winrt::get_unknown(customButton)->AddRef();
            int num = winrt::get_unknown(customButton)->Release();
            result2 = (num >= 1);
        }
        else if (customButtonWeak != nullptr)
        {
            result2 = (customButtonWeak.get() != nullptr);
        }

        labelCustom().Text(L"Custom Button isAlive in C#: " + to_hstring(result) + L", in C++: " + to_hstring(result2));
    }
}
