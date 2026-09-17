#pragma once
#include "DeprecatedConstructorClass.g.h"
#include "RemovedActivationClass.g.h"
#include "RemovedComposableClass.g.h"

namespace winrt::TestComponentCSharp::implementation
{
    // Each constructor stores a distinct value so a test can tell which factory slot was
    // actually dispatched to (the removed overloads keep their slot, but are not projected).
    struct DeprecatedConstructorClass : DeprecatedConstructorClassT<DeprecatedConstructorClass>
    {
        DeprecatedConstructorClass(int32_t first);
        DeprecatedConstructorClass(int32_t first, int32_t second);
        DeprecatedConstructorClass(int32_t first, int32_t second, int32_t third);

        int32_t Value();

    private:
        int32_t m_value{ 0 };
    };

    struct RemovedActivationClass : RemovedActivationClassT<RemovedActivationClass>
    {
        RemovedActivationClass(int32_t initialValue);

        static TestComponentCSharp::RemovedActivationClass Create(int32_t initialValue);

        int32_t Value();

    private:
        int32_t m_value{ 0 };
    };

    struct RemovedComposableClass : RemovedComposableClassT<RemovedComposableClass>
    {
        // The parameterless constructor is the one the (removed) composable factory method uses;
        // the other is implementation-only, for the static factory method below
        RemovedComposableClass() = default;
        explicit RemovedComposableClass(int32_t initialValue);

        static TestComponentCSharp::RemovedComposableClass Create(int32_t initialValue);

        int32_t Value();

    private:
        int32_t m_value{ 0 };
    };
}
namespace winrt::TestComponentCSharp::factory_implementation
{
    struct DeprecatedConstructorClass : DeprecatedConstructorClassT<DeprecatedConstructorClass, implementation::DeprecatedConstructorClass>
    {
    };

    struct RemovedActivationClass : RemovedActivationClassT<RemovedActivationClass, implementation::RemovedActivationClass>
    {
    };

    struct RemovedComposableClass : RemovedComposableClassT<RemovedComposableClass, implementation::RemovedComposableClass>
    {
    };
}
