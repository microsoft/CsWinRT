using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ABI.System.Numerics
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Matrix3x2
    {
        public static string GetGuidSignature() =>
            "struct(Windows.Foundation.Numerics.Matrix3x2;f4;f4;f4;f4;f4;f4)";
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Matrix4x4
    {
        public static string GetGuidSignature() =>
            "struct(Windows.Foundation.Numerics.Matrix4x4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4;f4)";
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Plane
    {
        public static string GetGuidSignature() =>
            "struct(Windows.Foundation.Numerics.Plane;struct(Windows.Foundation.Numerics.Vector3;f4;f4;f4);f4)";
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Quaternion
    {
        public static string GetGuidSignature() =>
            "struct(Windows.Foundation.Numerics.Quaternion;f4;f4;f4;f4)";
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Vector2
    {
        public static string GetGuidSignature() =>
            "struct(Windows.Foundation.Numerics.Vector2;f4;f4)";
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Vector3
    {
        public static string GetGuidSignature() =>
            "struct(Windows.Foundation.Numerics.Vector3;f4;f4;f4)";
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Vector4
    {
        public static string GetGuidSignature() =>
            "struct(Windows.Foundation.Numerics.Vector4;f4;f4;f4;f4)";
    }
}
