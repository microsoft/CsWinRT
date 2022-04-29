#include "pch.h"
#include "CustomEquals.h"
#include "CustomEquals.g.cpp"
#include "CustomEquals2.g.cpp"
#include "UnSealedCustomEquals.g.cpp"
#include "DerivedCustomEquals.g.cpp"

namespace winrt::TestComponentCSharp::implementation
{
    int32_t CustomEquals::Value()
    {
        return value;
    }

    void CustomEquals::Value(int32_t value)
    {
        this->value = value;
    }

    // Returns true as long as it is the same type (CustomEquals).
    bool CustomEquals::Equals(winrt::Windows::Foundation::IInspectable const& obj)
    {
        if (obj == nullptr)
        {
            return false;
        }

        auto customEqualsObj = obj.try_as<CustomEquals>();
        if (customEqualsObj)
        {
            return true;
        }

        return false;
    }

    int32_t CustomEquals::GetHashCode()
    {
        return 5;
    }

    // Returns true if Value is the same in both.
    bool CustomEquals::Equals(winrt::TestComponentCSharp::CustomEquals const& other)
    {
        return other != nullptr && Value() == other.Value();
    }

    int32_t CustomEquals2::Value()
    {
        return value;
    }

    void CustomEquals2::Value(int32_t value)
    {
        this->value = value;
    }

    int32_t CustomEquals2::Equals(winrt::Windows::Foundation::IInspectable const& obj)
    {
        return value;
    }

    int32_t CustomEquals2::Equals(winrt::TestComponentCSharp::CustomEquals2 const& other)
    {
        return value;
    }

    int32_t UnSealedCustomEquals::Value()
    {
        return value;
    }

    void UnSealedCustomEquals::Value(int32_t value)
    {
        this->value = value;
    }

    int32_t UnSealedCustomEquals::GetHashCode()
    {
        return 8;
    }

    bool DerivedCustomEquals::Equals(winrt::Windows::Foundation::IInspectable const& obj)
    {
        if (obj == nullptr)
        {
            return false;
        }

        auto customEqualsObj = obj.try_as<DerivedCustomEquals>();
        if (customEqualsObj)
        {
            return true;
        }

        return false;
    }

    int32_t DerivedCustomEquals::GetHashCode()
    {
        return 10;
    }

    bool DerivedCustomEquals::Equals(winrt::TestComponentCSharp::UnSealedCustomEquals const& other)
    {
        return other != nullptr && Value() == other.Value();
    }

}
