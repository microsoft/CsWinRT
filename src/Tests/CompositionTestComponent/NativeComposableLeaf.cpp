#include "pch.h"
#include "NativeComposableLeaf.h"
#include "NativeComposableLeaf.g.cpp"

namespace winrt::CompositionTestComponent::implementation
{
    NativeComposableLeaf::NativeComposableLeaf()
    {
    }

    NativeComposableLeaf::NativeComposableLeaf(int32_t value)
        : leaf_base_type(value)
        , m_leafValue(value)
    {
    }

    int32_t NativeComposableLeaf::GetLeafValue()
    {
        return m_leafValue * 1000;
    }

    int32_t NativeComposableLeaf::ComputeMiddleValue()
    {
        return NativeComposableMiddle::ComputeMiddleValue() + 11;
    }

    hstring NativeComposableLeaf::DescribeMiddleCore()
    {
        return L"NativeLeaf:" + NativeComposableMiddle::DescribeMiddleCore();
    }
}
