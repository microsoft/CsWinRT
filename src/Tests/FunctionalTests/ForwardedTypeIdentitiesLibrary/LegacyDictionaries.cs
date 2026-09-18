// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !NETSTANDARD1_3
#error This helper must target netstandard1.3 so ReadOnlyDictionary is referenced through System.ObjectModel.
#endif

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ForwardedTypeIdentitiesLibrary;

public static class LegacyDictionaries
{
    public static ReadOnlyDictionary<string, object> CreateObjects(object value)
    {
        return new(new Dictionary<string, object> { ["key"] = value });
    }

    public static ReadOnlyDictionary<string, string> CreateStrings(string value)
    {
        return new(new Dictionary<string, string> { ["key"] = value });
    }
}
