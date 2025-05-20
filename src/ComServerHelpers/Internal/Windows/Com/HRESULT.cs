// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices.Marshalling;
using ComServerHelpers.Internal.Windows.Com.Marshalling;

namespace Windows.Win32.Foundation;

[NativeMarshalling(typeof(HResultMarshaller))]
partial struct HRESULT;
