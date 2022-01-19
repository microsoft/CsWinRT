// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace ABI.System.Numerics
{
    [StructLayout(LayoutKind.Sequential)]
#if EMBED
    internal
#else
    public
#endif
    struct Matrix3x2
    {
        public static string GetGuidSignature() =>
            "struct(Windows.Foundation.Numerics.Matrix3x2;f4;f4;f4;f4;f4;f4)";
    }

    [StructLayout(LayoutKind.Sequential)]
#if EMBED
    internal
#else
    public
#endif
    struct Matrix4x4
    {
        public static string GetGuidSignature() =>
            "struct(Windows.Foundation.Numerics.Matrix4x4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4)";
    }

    [StructLayout(LayoutKind.Sequential)]
#if EMBED
    internal
#else
    public
#endif
    struct Plane
    {
        public static string GetGuidSignature() =>
            "struct(Windows.Foundation.Numerics.Plane;struct(Windows.Foundation.Numerics.Vector3;f4;f4;f4);f4)";
    }

    [StructLayout(LayoutKind.Sequential)]
#if EMBED
    internal
#else
    public
#endif
    struct Quaternion
    {
        public static string GetGuidSignature() =>
            "struct(Windows.Foundation.Numerics.Quaternion;f4;f4;f4;f4)";
    }

    [StructLayout(LayoutKind.Sequential)]
#if EMBED
    internal
#else
    public
#endif
    struct Vector2
    {
        public static string GetGuidSignature() =>
            "struct(Windows.Foundation.Numerics.Vector2;f4;f4)";
    }

    [StructLayout(LayoutKind.Sequential)]
#if EMBED
    internal
#else
    public
#endif
    struct Vector3
    {
        public static string GetGuidSignature() =>
            "struct(Windows.Foundation.Numerics.Vector3;f4;f4;f4)";
    }

    [StructLayout(LayoutKind.Sequential)]
#if EMBED
    internal
#else
    public
#endif
    struct Vector4
    {
        public static string GetGuidSignature() =>
            "struct(Windows.Foundation.Numerics.Vector4;f4;f4;f4;f4)";
    }
}
