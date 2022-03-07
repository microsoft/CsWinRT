// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "pch.h"
#include "iostream"

using namespace winrt;
using namespace Windows::Foundation;
using namespace std;

int main()
{
    init_apartment();

    AuthoringDemo::Example ex;
    ex.SampleProperty(42);
    wcout << ex.SampleProperty() << endl;
    wcout << ex.SayHello().c_str() << endl;

    AuthoringDemo::FolderEnumeration folderEnumerator;
    folderEnumerator.GetFilesAndFoldersAsync().get();
    wcout << folderEnumerator.AllFiles().c_str() << endl;
}
