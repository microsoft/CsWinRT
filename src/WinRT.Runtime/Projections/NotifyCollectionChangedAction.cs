// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WinRT;

namespace ABI.System.Collections.Specialized
{
    static class NotifyCollectionChangedAction
    {
        public static string GetGuidSignature() =>
            FeatureSwitches.IsWuxMode
            ? "enum(Windows.UI.Xaml.Interop.NotifyCollectionChangedAction;i4)"
            : "enum(Microsoft.UI.Xaml.Interop.NotifyCollectionChangedAction;i4)";
    }
}
