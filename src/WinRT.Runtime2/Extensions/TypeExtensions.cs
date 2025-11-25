// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.InteropServices;

namespace WindowsRuntime.Extensions;

/// <summary>
/// Provides extension methods for the <see cref="System.Type"/> class.
/// </summary>
public static class TypeExtensions
{
    /// <summary>
    /// Determines whether the specified type is a Windows Runtime type.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the type is a Windows Runtime type; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsTypeWindowsRuntimeType(this global::System.Type systemType)
    {
        return IsTypeWindowsRuntimeTypeNoArray(systemType.GetElementType() ?? systemType);
    }

    private static bool IsTypeWindowsRuntimeTypeNoArray(this global::System.Type systemType)
    {
        // We might not need to handle Generic Types because WindowsRuntimeMarshallingInfo should have all the possible generic types with types from CSWinRTGen
        // But uncomment these lines if we need to handle them in the future
        // TODO: Confirm we don't need to handle Generic Types here and remove all these comments
        //if (systemType.IsConstructedGenericType)
        //{
        //    if (IsTypeWindowsRuntimeTypeNoArray(systemType.GetGenericTypeDefinition()))
        //    {
        //        foreach (System.Type arg in systemType.GetGenericArguments())
        //        {
        //            if (!IsTypeWindowsRuntimeTypeNoArray(arg))
        //            {
        //                return false;
        //            }
        //        }
        //        return true;
        //    }
        //    return false;
        //}

        return systemType.IsPrimitive
            || systemType == typeof(string)
            || systemType == typeof(object)
            || systemType == typeof(Guid)
            || WindowsRuntimeMarshallingInfo.TryGetInfo(systemType, out _);
        // TODO: Verify Authoring Scenarios
    }

}
