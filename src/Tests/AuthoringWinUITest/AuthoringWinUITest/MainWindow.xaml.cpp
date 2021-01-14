#include "pch.h"
#include "MainWindow.xaml.h"
#if __has_include("MainWindow.g.cpp")
#include "MainWindow.g.cpp"
#endif

using namespace winrt;
using namespace Microsoft::UI::Xaml;
using namespace AuthoringTest;

namespace winrt::AuthoringWinUITest::implementation
{
    std::wstring GetLastCustomButtonString(Microsoft::UI::Xaml::Controls::UIElementCollection buttons)
    {
        std::wstring str = L"";
        for (auto button : buttons)
        {
            if (auto customButton = button.try_as<CustomButton>())
            {
                str = customButton.GetText().c_str();
            }
        }

        if (str != L"Custom row 5")
        {
            throw winrt::hresult_error();
        }

        return str;
    }

    MainWindow::MainWindow()
    {
        InitializeComponent();

        myStackPanel().Children().Append(ButtonUtils::GetButton());
        myStackPanel().Children().Append(ButtonUtils::GetCustomButton());
        myStackPanel().Children().Append(ButtonUtils::GetCustomButton(L"Custom row 4"));
        myStackPanel().Children().Append(CustomButton());
        myStackPanel().Children().Append(CustomButton(L"Custom row 5"));
        myStackPanel().Children().Append(CustomButton(GetLastCustomButtonString(myStackPanel().Children())));
    }

    void MainWindow::myButton_Click(IInspectable const&, RoutedEventArgs const&)
    {
        myButton().Content(box_value(L"Clicked"));
    }
}
