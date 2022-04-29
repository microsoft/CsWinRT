#pragma once
#include "CustomEquals.g.h"
#include "CustomEquals2.g.h"
#include "UnSealedCustomEquals.g.h"
#include "DerivedCustomEquals.g.h"

namespace winrt::TestComponentCSharp::implementation
{
    struct CustomEquals : CustomEqualsT<CustomEquals>
    {
        CustomEquals() = default;

        int32_t Value();
        void Value(int32_t value);
        bool Equals(winrt::Windows::Foundation::IInspectable const& obj);
        int32_t GetHashCode();
        bool Equals(winrt::TestComponentCSharp::CustomEquals const& other);

    private:
        int32_t value;
    };

    struct CustomEquals2 : CustomEquals2T<CustomEquals2>
    {
        CustomEquals2() = default;

        int32_t Value();
        void Value(int32_t value);
        int32_t Equals(winrt::Windows::Foundation::IInspectable const& obj);
        int32_t Equals(winrt::TestComponentCSharp::CustomEquals2 const& other);

    private:
        int32_t value;
    };

    struct UnSealedCustomEquals : UnSealedCustomEqualsT<UnSealedCustomEquals>
    {
        UnSealedCustomEquals() = default;

        int32_t Value();
        void Value(int32_t value);
        int32_t GetHashCode();

    private:
        int32_t value;
    };

    struct DerivedCustomEquals : DerivedCustomEqualsT<DerivedCustomEquals, TestComponentCSharp::implementation::UnSealedCustomEquals>
    {
        DerivedCustomEquals() = default;

        bool Equals(winrt::Windows::Foundation::IInspectable const& obj);
        int32_t GetHashCode();
        bool Equals(winrt::TestComponentCSharp::UnSealedCustomEquals const& other);
    };
}
namespace winrt::TestComponentCSharp::factory_implementation
{
    struct CustomEquals : CustomEqualsT<CustomEquals, implementation::CustomEquals>
    {
    };

    struct CustomEquals2 : CustomEquals2T<CustomEquals2, implementation::CustomEquals2>
    {
    };

    struct UnSealedCustomEquals : UnSealedCustomEqualsT<UnSealedCustomEquals, implementation::UnSealedCustomEquals>
    {
    };

    struct DerivedCustomEquals : DerivedCustomEqualsT<DerivedCustomEquals, implementation::DerivedCustomEquals>
    {
    };
}