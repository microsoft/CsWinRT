#include "pch.h"
#include "ManualProjectionTestClasses.h"
#include "CustomBindableIteratorTest.g.cpp"
#include "CustomDisposableTest.g.cpp"
#include "CustomBindableVectorTest.g.cpp"

namespace winrt::TestComponentCSharp::implementation
{
	CustomBindableIteratorTest::CustomBindableIteratorTest()
	{
		
	}

	bool CustomBindableIteratorTest::MoveNext()
	{
		return true;
	}

	Windows::Foundation::IInspectable CustomBindableIteratorTest::Current()
	{
		return Windows::Foundation::PropertyValue::CreateInt32(27861);
	}

	bool CustomBindableIteratorTest::HasCurrent()
	{
		return true;
	}

	CustomDisposableTest::CustomDisposableTest()
	{
	}

	void CustomDisposableTest::Close()
	{
	}

    CustomBindableVectorTest::CustomBindableVectorTest()
    {
    }
    void CustomBindableVectorTest::Clear()
    {
    }
    void CustomBindableVectorTest::Append(Windows::Foundation::IInspectable value)
    {
    }
    Windows::Foundation::IInspectable CustomBindableVectorTest::GetAt(int32_t index)
    {
        return Windows::Foundation::PropertyValue::CreateInt32(1);
    }
    winrt::Microsoft::UI::Xaml::Interop::IBindableVectorView CustomBindableVectorTest::GetView()
    {
        return winrt::Microsoft::UI::Xaml::Interop::IBindableVectorView();
    }
    bool CustomBindableVectorTest::IndexOf(Windows::Foundation::IInspectable, uint32_t& index)
    {
        return false;
    }
    void CustomBindableVectorTest::InsertAt(int32_t index, Windows::Foundation::IInspectable value)
    {
    }
    void CustomBindableVectorTest::RemoveAt(int32_t index)
    {
    }
    void CustomBindableVectorTest::RemoveAtEnd()
    {
    }
    void CustomBindableVectorTest::SetAt(int32_t index, Windows::Foundation::IInspectable value)
    {
    }
    int32_t CustomBindableVectorTest::Size()
    {
        return 1;
    }
    winrt::Microsoft::UI::Xaml::Interop::IBindableIterator CustomBindableVectorTest::First()
    {
        return winrt::Microsoft::UI::Xaml::Interop::IBindableIterator();
    }
}