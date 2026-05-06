#include "pch.h"

using namespace winrt;
using namespace Windows::Foundation;

int main()
{
    init_apartment();
    Uri uri(L"http://aka.ms/cppwinrt");
    printf("Hello, %ls!\n", uri.AbsoluteUri().c_str());

    // Activate a runtime class from ClassLibrary1.
    ClassLibrary1::Class1 class1;
    printf("ClassLibrary1.Class1.Add(1, 2) = %d\n", class1.Add(1, 2));
    printf("ClassLibrary1.Class1.Multiply(3, 4) = %d\n", class1.Multiply(3, 4));

    // Activate a runtime class from ClassLibrary2 (different component dll).
    // This exercises the merged dispatcher in WinRT.Component.dll: WinRT.Host.dll
    // routes both runtime class names to WinRT.Component.dll, whose merged
    // ABI.WinRT.Component.ManagedExports.GetActivationFactory dispatches to the
    // correct per-component ABI.{Name}.ManagedExports.GetActivationFactory.
    ClassLibrary2::Greeter greeter;
    auto greeting = greeter.Greet(L"world");
    printf("ClassLibrary2.Greeter.Greet(...) = %ls\n", greeting.c_str());
    auto loud = greeter.Shout(L"can you hear me");
    printf("ClassLibrary2.Greeter.Shout(...) = %ls\n", loud.c_str());
}