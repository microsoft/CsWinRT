#include "pch.h"
#include "MainWindow.xaml.h"
#if __has_include("MainWindow.g.cpp")
#include "MainWindow.g.cpp"
#endif

using namespace winrt;
using namespace Microsoft::UI::Xaml;
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
    }

    void MainWindow::acquireCustomButton_Click(IInspectable const&, RoutedEventArgs const&)
    {
        customButton = TestButtons::GetCustomButton();
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

    void MainWindow::isAliveRegularButton_Click(IInspectable const& sender, RoutedEventArgs const& args)
    {
        bool result = TestButtons::IsAliveRegularButton();
        labelRegular().Text(L"Regular Button isAlive: " + to_hstring(result));
    }

    void MainWindow::isAliveCustomButton_Click(IInspectable const&, RoutedEventArgs const&)
    {
        bool result = TestButtons::IsAliveCustomButton();
        labelCustom().Text(L"Custom Button isAlive: " + to_hstring(result));
    }
}
