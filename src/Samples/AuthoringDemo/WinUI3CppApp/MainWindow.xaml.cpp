#include "pch.h"
#include "MainWindow.xaml.h"
#if __has_include("MainWindow.g.cpp")
#include "MainWindow.g.cpp"
#endif

using namespace winrt;
using namespace Microsoft::UI::Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace winrt::WinUI3CppApp::implementation
{
    MainWindow::MainWindow()
    {
        InitializeComponent();
    }

    void MainWindow::sayHelloButton_Click(IInspectable const&, RoutedEventArgs const&)
    {
        AuthoringDemo::Example ex;
        sayHelloButton().Content(box_value(ex.SayHello()));
    }
}
