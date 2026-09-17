#include "pch.h"
#include "DeprecatedClasses.h"
#include "DeprecatedConstructorClass.g.cpp"
#include "RemovedActivationClass.g.cpp"
#include "RemovedComposableClass.g.cpp"

namespace winrt::TestComponentCSharp::implementation
{
    // The value encodes which constructor ran, so a test can prove the surviving constructor still
    // dispatches through its original factory slot even though the one before it was removed.
    DeprecatedConstructorClass::DeprecatedConstructorClass(int32_t first)
    {
        m_value = first;
    }

    DeprecatedConstructorClass::DeprecatedConstructorClass(int32_t first, int32_t second)
    {
        m_value = first + second;
    }

    DeprecatedConstructorClass::DeprecatedConstructorClass(int32_t first, int32_t second, int32_t third)
    {
        m_value = first + second + third;
    }

    int32_t DeprecatedConstructorClass::Value()
    {
        return m_value;
    }

    RemovedActivationClass::RemovedActivationClass(int32_t initialValue)
    {
        m_value = initialValue;
    }

    TestComponentCSharp::RemovedActivationClass RemovedActivationClass::Create(int32_t initialValue)
    {
        return winrt::make<RemovedActivationClass>(initialValue);
    }

    int32_t RemovedActivationClass::Value()
    {
        return m_value;
    }

    TestComponentCSharp::RemovedComposableClass RemovedComposableClass::Create(int32_t initialValue)
    {
        return winrt::make<RemovedComposableClass>(initialValue);
    }

    RemovedComposableClass::RemovedComposableClass(int32_t initialValue)
    {
        m_value = initialValue;
    }

    int32_t RemovedComposableClass::Value()
    {
        return m_value;
    }
}
