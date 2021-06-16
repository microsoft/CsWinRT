#pragma once
#include "Windows.Class.g.h"

namespace winrt::TestComponentCSharp::Windows::implementation
{
    struct Class : ClassT<Class>
    {
        Class() = default;

        static void StaticMethod();
        void Method();
        void Method2(winrt::Windows::Foundation::IStringable const& stringable);
    };
}
namespace winrt::TestComponentCSharp::Windows::factory_implementation
{
    struct Class : ClassT<Class, implementation::Class>
    {
    };
}
