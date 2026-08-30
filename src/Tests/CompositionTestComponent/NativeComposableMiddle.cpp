#include "pch.h"
#include "NativeComposableMiddle.h"
#include "NativeComposableMiddle.g.cpp"

namespace winrt::CompositionTestComponent::implementation
{
    NativeComposableMiddle::NativeComposableMiddle()
        : NativeComposableMiddleT<NativeComposableMiddle>()
    {
        ++s_liveInstanceCount;
    }

    NativeComposableMiddle::NativeComposableMiddle(int32_t value)
        : NativeComposableMiddleT<NativeComposableMiddle>(value)
        , m_value(value)
    {
        ++s_liveInstanceCount;
    }

    NativeComposableMiddle::~NativeComposableMiddle()
    {
        --s_liveInstanceCount;
        ++s_destroyedInstanceCount;
    }

    int32_t NativeComposableMiddle::GetMiddleValue()
    {
        return m_value * 100;
    }

    hstring NativeComposableMiddle::MiddleTag()
    {
        return m_middleTag;
    }

    void NativeComposableMiddle::MiddleTag(hstring const& value)
    {
        m_middleTag = value;
    }

    hstring NativeComposableMiddle::DescribeMiddle()
    {
        return L"NativeComposableMiddle(" + winrt::to_hstring(m_value) + L")";
    }

    int32_t NativeComposableMiddle::GetMiddleSecretValue()
    {
        return m_value * 7;
    }

    hstring NativeComposableMiddle::MiddleSecretTag()
    {
        return m_middleSecretTag;
    }

    void NativeComposableMiddle::MiddleSecretTag(hstring const& value)
    {
        m_middleSecretTag = value;
    }

    int32_t NativeComposableMiddle::ComputeMiddleValue()
    {
        ++s_computeMiddleValueCallCount;

        return m_value * 3;
    }

    hstring NativeComposableMiddle::DescribeMiddleCore()
    {
        return L"NativeMiddle:" + winrt::to_hstring(m_value);
    }

    int32_t NativeComposableMiddle::CallComputeMiddleValue()
    {
        return overridable().ComputeMiddleValue();
    }

    hstring NativeComposableMiddle::CallDescribeMiddleCore()
    {
        return overridable().DescribeMiddleCore();
    }

    int32_t NativeComposableMiddle::CallOwnComputeMiddleValue()
    {
        return NativeComposableMiddle::ComputeMiddleValue();
    }

    hstring NativeComposableMiddle::CallOwnDescribeMiddleCore()
    {
        return NativeComposableMiddle::DescribeMiddleCore();
    }

    int32_t NativeComposableMiddle::CallBaseSecretValue()
    {
        return this->GetSecretValue();
    }

    hstring NativeComposableMiddle::GetBaseSecretTag()
    {
        return this->SecretTag();
    }

    void NativeComposableMiddle::SetBaseSecretTag(hstring const& value)
    {
        this->SecretTag(value);
    }

    hstring NativeComposableMiddle::CallBaseDescribeSelf()
    {
        return this->DescribeSelf();
    }

    int32_t NativeComposableMiddle::CallBaseGetValue()
    {
        return this->GetValue();
    }

    int32_t NativeComposableMiddle::CallBaseComputeValue()
    {
        return base_type::ComputeValue();
    }

    hstring NativeComposableMiddle::CallBaseDescribeCore()
    {
        return base_type::DescribeCore();
    }

    int32_t NativeComposableMiddle::CallBaseOverridableValue()
    {
        return base_type::OverridableValue();
    }

    int32_t NativeComposableMiddle::CallBaseComputeCoreValue()
    {
        return base_type::ComputeCoreValue();
    }

    int32_t NativeComposableMiddle::CallBaseCallComputeCoreValue()
    {
        return this->CallComputeCoreValue();
    }

    int32_t NativeComposableMiddle::ComputeValue()
    {
        return base_type::ComputeValue() + 5;
    }

    int32_t NativeComposableMiddle::ComputeCoreValue()
    {
        ++s_computeCoreValueCallCount;

        return base_type::ComputeCoreValue() + 3;
    }

    Windows::Foundation::IInspectable NativeComposableMiddle::GetSelfAsObject()
    {
        return *this;
    }

    CompositionTestComponent::NativeComposableMiddle NativeComposableMiddle::GetSelfAsMiddle()
    {
        return *this;
    }

    winrt::AuthoringTest::ComposableBase NativeComposableMiddle::GetSelfAsBaseClass()
    {
        return this->try_as<winrt::AuthoringTest::ComposableBase>();
    }

    bool NativeComposableMiddle::IsSameMiddle(CompositionTestComponent::NativeComposableMiddle const& other)
    {
        CompositionTestComponent::NativeComposableMiddle self = *this;

        return winrt::get_abi(self.as<Windows::Foundation::IUnknown>()) ==
            winrt::get_abi(other.as<Windows::Foundation::IUnknown>());
    }

    int32_t NativeComposableMiddle::LiveInstanceCount()
    {
        return s_liveInstanceCount;
    }

    int32_t NativeComposableMiddle::DestroyedInstanceCount()
    {
        return s_destroyedInstanceCount;
    }

    int32_t NativeComposableMiddle::ComputeMiddleValueCallCount()
    {
        return s_computeMiddleValueCallCount;
    }

    int32_t NativeComposableMiddle::MiddleComputeCoreValueCallCount()
    {
        return s_computeCoreValueCallCount;
    }

    void NativeComposableMiddle::ResetMiddleCallCounts()
    {
        s_destroyedInstanceCount = 0;
        s_computeMiddleValueCallCount = 0;
        s_computeCoreValueCallCount = 0;
    }

    hstring NativeComposableMiddle::DescribeMiddleValue(int32_t middleValue)
    {
        return L"NativeComposableMiddle(" + winrt::to_hstring(middleValue) + L")";
    }
}
