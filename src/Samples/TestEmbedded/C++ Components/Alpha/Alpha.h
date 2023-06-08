#pragma once

#include "Class.g.h"

namespace winrt::Alpha::implementation
{
    struct Class : ClassT<Class>
    {
        Class() = default;

        winrt::Windows::Foundation::Collections::IVector<hstring> GetStringList();
        winrt::Windows::Foundation::Collections::IVector<int> GetIntList();
        winrt::Windows::Foundation::Collections::IVectorView<winrt::Alpha::Class> GetObjectList();
    };
}

namespace winrt::Alpha::factory_implementation
{
    struct Class : ClassT<Class, implementation::Class>
    {
    };
}
