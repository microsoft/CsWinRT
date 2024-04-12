#pragma once
#include "WinRT.Class.g.h"

namespace winrt::TestComponentCSharp::WinRT::implementation
{
    struct Class : ClassT<Class>
    {
        Class() = default;

        static void StaticMethod();
        void Method();
        void Method2(winrt::Windows::Foundation::IStringable const& stringable);
    };
}
namespace winrt::TestComponentCSharp::WinRT::factory_implementation
{
    struct Class : ClassT<Class, implementation::Class>
    {
    };
}
