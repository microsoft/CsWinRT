#include "pch.h"
#include "CastMetadata.h"
#include "CastMetadata.Class.g.cpp"
#include "CastMetadata.ClassFactory.g.cpp"

namespace winrt::TestComponentCSharp::CastMetadata::implementation
{
	winrt::Windows::Foundation::IInspectable ClassFactory::Create()
	{
		return winrt::make<Class>();
	}
}