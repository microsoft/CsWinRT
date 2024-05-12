#pragma once
#include "ClassWithExplicitIUnknown.g.h"

namespace winrt::TestComponentCSharp::implementation
{
    // Explicity referencing IUnknown.
    struct ClassWithExplicitIUnknown : ClassWithExplicitIUnknownT<ClassWithExplicitIUnknown, IUnknown>
    {
        ClassWithExplicitIUnknown() = default;

        ClassWithExplicitIUnknown(int32_t defaultValue);
        int32_t Value();
        void Value(int32_t value);

    private:
        int32_t m_value{ 0 };
    };
}
namespace winrt::TestComponentCSharp::factory_implementation
{
    struct ClassWithExplicitIUnknown : ClassWithExplicitIUnknownT<ClassWithExplicitIUnknown, implementation::ClassWithExplicitIUnknown>
    {
    };
}
