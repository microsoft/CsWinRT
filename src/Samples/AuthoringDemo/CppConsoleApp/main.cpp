#include "pch.h"
#include "iostream"

using namespace winrt;
using namespace Windows::Foundation;

int main()
{
    init_apartment();

    AuthoringDemo::Example ex;
    ex.SampleProperty(42);
    std::wcout << ex.SampleProperty() << std::endl;
    std::wcout << ex.SayHello().c_str() << std::endl;

    AuthoringDemo::FolderEnumeration folderEnumerator;
    folderEnumerator.GetFilesAndFoldersAsync().get();
    std::wcout << folderEnumerator.AllFiles().c_str() << std::endl;
}
