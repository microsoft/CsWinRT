#include "pch.h"
#include "ManualProjectionTestClasses.h"
#include "CustomBindableIteratorTest.g.cpp"
#include "CustomDisposableTest.g.cpp"
#include "CustomBindableVectorTest.g.cpp"
#include "CustomBindableObservableVectorTest.g.cpp"
#include "CustomIteratorTest.g.cpp"
#include "SetTypeProperties.g.cpp"
#include <winrt/Windows.UI.Xaml.Interop.h>

namespace winrt::TestComponentCSharp::implementation
{
    SetTypeProperties::SetTypeProperties()
    {

    }

    winrt::hstring SetTypeProperties::GetPropertyInfoWithIType(IType testObject)
    {
        testObject.TypeProperty(winrt::xaml_typename<TestComponentCSharp::TestType1>());
        winrt::hstring kind;
        switch (testObject.TypeProperty().Kind)
        {
            case Windows::UI::Xaml::Interop::TypeKind::Custom:
                kind = winrt::hstring(L"Custom");
            case Windows::UI::Xaml::Interop::TypeKind::Metadata:
                kind = winrt::hstring(L"Metadata");
            default:
                kind = winrt::hstring(L"Primitive");
        }
        return testObject.TypeProperty().Name + L" " + kind;
    }

    winrt::hstring SetTypeProperties::GetPropertyInfoFromCustomType(winrt::Windows::UI::Xaml::Interop::TypeName typeName)
    {
        TestComponentCSharp::Class TestObject;
        TestObject.TypeProperty(typeName);
        return TestObject.GetTypePropertyAbiName() + L" " + TestObject.GetTypePropertyKind();
    }

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
        // Leaving in for testing purposes
        // throw winrt::hresult_access_denied();
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

    winrt::Microsoft::UI::Xaml::Interop::IBindableIterator CustomBindableObservableVectorTest::First()
    {
        return winrt::Microsoft::UI::Xaml::Interop::IBindableIterator();
    }
    winrt::Windows::Foundation::IInspectable CustomBindableObservableVectorTest::GetAt(uint32_t index)
    {
        return Windows::Foundation::PropertyValue::CreateInt32(1);
    }
    uint32_t CustomBindableObservableVectorTest::Size()
    {
        return 1;
    }
    winrt::Microsoft::UI::Xaml::Interop::IBindableVectorView CustomBindableObservableVectorTest::GetView()
    {
        return winrt::Microsoft::UI::Xaml::Interop::IBindableVectorView();
    }
    bool CustomBindableObservableVectorTest::IndexOf(winrt::Windows::Foundation::IInspectable const& value, uint32_t& index)
    {
        return false;
    }
    void CustomBindableObservableVectorTest::SetAt(uint32_t index, winrt::Windows::Foundation::IInspectable const& value)
    {
    }
    void CustomBindableObservableVectorTest::InsertAt(uint32_t index, winrt::Windows::Foundation::IInspectable const& value)
    {
    }
    void CustomBindableObservableVectorTest::RemoveAt(uint32_t index)
    {
    }
    void CustomBindableObservableVectorTest::Append(winrt::Windows::Foundation::IInspectable const& value)
    {
    }
    void CustomBindableObservableVectorTest::RemoveAtEnd()
    {
    }
    void CustomBindableObservableVectorTest::Clear()
    {
    }
    winrt::event_token CustomBindableObservableVectorTest::VectorChanged(winrt::Microsoft::UI::Xaml::Interop::BindableVectorChangedEventHandler const& handler)
    {
        throw hresult_not_implemented();
    }
    void CustomBindableObservableVectorTest::VectorChanged(winrt::event_token const& token) noexcept
    {
    }

    CustomIteratorTest::CustomIteratorTest(winrt::Windows::Foundation::Collections::IIterator<int> iterator)
    {
        _iterator = iterator;
    }
    int32_t CustomIteratorTest::Current()
    {
        if (_iterator)
        {
            return _iterator.Current();
        }

        return 2;
    }
    bool CustomIteratorTest::HasCurrent()
    {
        if (_iterator)
        {
            return _iterator.HasCurrent();
        }

        return true;
    }
    bool CustomIteratorTest::MoveNext()
    {
        if (_iterator)
        {
            return _iterator.MoveNext();
        }

        return true;
    }
    uint32_t CustomIteratorTest::GetMany(array_view<int32_t> items)
    {
        if (_iterator)
        {
            return _iterator.GetMany(items);
        }

        throw hresult_not_implemented();
    }
}