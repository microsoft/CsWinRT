#pragma once
#include "NativeComposableLeaf.g.h"
#include "NativeComposableMiddle.h"

namespace winrt::CompositionTestComponent::implementation
{
    // A sealed native class deriving from the unsealed native middle class, so tests can compare the
    // native and the C# derivation of the same composable middle class. C++/WinRT derives from the
    // implementation type directly when the base class lives in the same component, so this is plain
    // C++ inheritance: the aggregate is still just the middle class composing the C# base.
    struct NativeComposableLeaf : NativeComposableLeafT<NativeComposableLeaf, implementation::NativeComposableMiddle>
    {
        using leaf_base_type = NativeComposableLeafT<NativeComposableLeaf, implementation::NativeComposableMiddle>;

        NativeComposableLeaf();
        explicit NativeComposableLeaf(int32_t value);

        int32_t GetLeafValue();

        // Overrides of the '[overridable]' members of the native middle class
        int32_t ComputeMiddleValue() override;
        hstring DescribeMiddleCore() override;

    private:
        int32_t m_leafValue{ 5 };
    };
}

namespace winrt::CompositionTestComponent::factory_implementation
{
    struct NativeComposableLeaf : NativeComposableLeafT<NativeComposableLeaf, implementation::NativeComposableLeaf>
    {
    };
}
