#include "pch.h"
#include "ClassWithExplicitIUnknown.h"
#include "ClassWithExplicitIUnknown.g.cpp"

namespace winrt::TestComponentCSharp::implementation
{
    ClassWithExplicitIUnknown::ClassWithExplicitIUnknown(int32_t defaultValue)
    {
        m_value = defaultValue;
    }

    int32_t ClassWithExplicitIUnknown::Value()
    {
        return m_value;
    }

    void ClassWithExplicitIUnknown::Value(int32_t value)
    {
        m_value = value;
    }
}
