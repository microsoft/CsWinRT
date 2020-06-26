#pragma once
#include "ComImports.g.h"

namespace winrt::TestComponentCSharp::implementation
{
    struct ComImports
    {
        ComImports() = default;

        static Windows::Foundation::IInspectable MakeObject();
        static int32_t NumObjects();
    };
}
namespace winrt::TestComponentCSharp::factory_implementation
{
    struct ComImports : ComImportsT<ComImports, implementation::ComImports>
    {
    };
}
