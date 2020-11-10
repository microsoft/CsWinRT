#include "pch.h"
#include "ManualProjectionTestClasses.h"
#include "CustomBindableIteratorTest.g.cpp"
#include "CustomDisposableTest.g.cpp"

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
}