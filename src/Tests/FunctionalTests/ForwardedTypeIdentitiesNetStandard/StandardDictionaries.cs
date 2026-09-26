// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !NETSTANDARD2_0
#error This helper must target netstandard2.0 so its framework types are referenced through netstandard.
#endif

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ForwardedTypeIdentitiesNetStandard;

public static class StandardDictionaries
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
