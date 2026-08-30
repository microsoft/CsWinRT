#pragma once
#include "NativeComposableMiddle.g.h"

namespace winrt::CompositionTestComponent::implementation
{
    // The middle layer of the composition chain. This class aggregates the C# authored
    // 'AuthoringTest.ComposableBase' (the inner object) and can itself be aggregated by a derived
    // C# class (the controlling outer object), so a fully constructed instance is a three level
    // COM aggregate: managed outer -> native middle -> managed inner.
    struct NativeComposableMiddle : NativeComposableMiddleT<NativeComposableMiddle>
    {
        using base_type = NativeComposableMiddleT<NativeComposableMiddle>;

        static inline int32_t s_liveInstanceCount{};
        static inline int32_t s_destroyedInstanceCount{};
        static inline int32_t s_computeMiddleValueCallCount{};
        static inline int32_t s_computeCoreValueCallCount{};

        NativeComposableMiddle();
        explicit NativeComposableMiddle(int32_t value);
        ~NativeComposableMiddle();

        // Plain members
        int32_t GetMiddleValue();
        hstring MiddleTag();
        void MiddleTag(hstring const& value);
        hstring DescribeMiddle();

        // '[protected]' members
        int32_t GetMiddleSecretValue();
        hstring MiddleSecretTag();
        void MiddleSecretTag(hstring const& value);

        // '[overridable]' members. These are 'virtual' so that a native class deriving from this one
        // in the same component (which C++/WinRT implements as plain C++ inheritance rather than COM
        // aggregation) can replace them too.
        virtual int32_t ComputeMiddleValue();
        virtual hstring DescribeMiddleCore();

        // Dispatch to the most derived implementation of the members above
        int32_t CallComputeMiddleValue();
        hstring CallDescribeMiddleCore();

        // Reach the implementation declared by this class
        int32_t CallOwnComputeMiddleValue();
        hstring CallOwnDescribeMiddleCore();

        // Reach the members of the C# base through the non delegating inner object
        int32_t CallBaseSecretValue();
        hstring GetBaseSecretTag();
        void SetBaseSecretTag(hstring const& value);
        hstring CallBaseDescribeSelf();
        int32_t CallBaseGetValue();
        int32_t CallBaseComputeValue();
        hstring CallBaseDescribeCore();
        int32_t CallBaseOverridableValue();
        int32_t CallBaseComputeCoreValue();
        int32_t CallBaseCallComputeCoreValue();

        // Overrides of the '[overridable]' members of the C# base. 'ComputeValue' comes from the
        // interface CsWinRT synthesizes out of the 'virtual' members of the class, and
        // 'ComputeCoreValue' from an '[overridable]' interface the C# component authors itself.
        int32_t ComputeValue();
        int32_t ComputeCoreValue();

        // COM identity helpers
        Windows::Foundation::IInspectable GetSelfAsObject();
        CompositionTestComponent::NativeComposableMiddle GetSelfAsMiddle();
        winrt::AuthoringTest::ComposableBase GetSelfAsBaseClass();
        bool IsSameMiddle(CompositionTestComponent::NativeComposableMiddle const& other);

        // Statics
        static int32_t LiveInstanceCount();
        static int32_t DestroyedInstanceCount();
        static int32_t ComputeMiddleValueCallCount();
        static int32_t MiddleComputeCoreValueCallCount();
        static void ResetMiddleCallCounts();
        static hstring DescribeMiddleValue(int32_t middleValue);

    private:
        int32_t m_value{ 5 };
        hstring m_middleTag{ L"middle" };
        hstring m_middleSecretTag{ L"middle-secret" };
    };
}

namespace winrt::CompositionTestComponent::factory_implementation
{
    struct NativeComposableMiddle : NativeComposableMiddleT<NativeComposableMiddle, implementation::NativeComposableMiddle>
    {
    };
}
