// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Windows.Foundation
{
    internal static class GSR
    {
        public static string ArgumentOutOfRange_NeedNonNegNum = "Non-negative number required.";
    }

    [global::WinRT.WindowsRuntimeType("Windows.Foundation.FoundationContract")]
    [global::WinRT.WindowsRuntimeHelperType(typeof(global::ABI.Windows.Foundation.Point))]
#if NET
    [global::WinRT.WinRTExposedType(typeof(global::WinRT.StructTypeDetails<Point, Point>))]
#endif
    [StructLayout(LayoutKind.Sequential)]
#if EMBED
    internal
#else
    public 
#endif
    struct Point : IFormattable
    {
        public float _x;
        public float _y;

        public Point(float x, float y)
        {
            _x = x;
            _y = y;
        }

        public Point(double x, double y) : this((float)x, (float)y) { }

        public double X
        {
            get { return _x; }
            set { _x = (float)value; }
        }

        public double Y
        {
            get { return _y; }
            set { _y = (float)value; }
        }

        public override string ToString()
        {
            return ConvertToString(null, null);
        }

        public string ToString(IFormatProvider provider)
        {
            return ConvertToString(null, provider);
        }

        string IFormattable.ToString(string format, IFormatProvider provider)
        {
            return ConvertToString(format, provider);
        }

        private string ConvertToString(string format, IFormatProvider provider)
        {
            char separator = GetNumericListSeparator(provider);
            return string.Format(provider, "{1:" + format + "}{0}{2:" + format + "}", separator, _x, _y);
        }

        static char GetNumericListSeparator(IFormatProvider provider)
        {
            // If the decimal separator is a comma use ';'
            char numericSeparator = ',';
            var numberFormat = NumberFormatInfo.GetInstance(provider);
            if ((numberFormat.NumberDecimalSeparator.Length > 0) && (numberFormat.NumberDecimalSeparator[0] == numericSeparator))
            {
                numericSeparator = ';';
            }

            return numericSeparator;
        }

        public static bool operator ==(Point point1, Point point2)
        {
            return point1._x == point2._x && point1._y == point2._y;
        }

        public static bool operator !=(Point point1, Point point2)
        {
            return !(point1 == point2);
        }

        public override bool Equals(object o)
        {
            return o is Point && this == (Point)o;
        }

        public bool Equals(Point value)
        {
            return (this == value);
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode();
        }
    }

    [global::WinRT.WindowsRuntimeType("Windows.Foundation.FoundationContract")]
    [global::WinRT.WindowsRuntimeHelperType(typeof(global::ABI.Windows.Foundation.Rect))]
#if NET
    [global::WinRT.WinRTExposedType(typeof(global::WinRT.StructTypeDetails<Rect, Rect>))]
#endif
    [StructLayout(LayoutKind.Sequential)]
#if EMBED
    internal
#else
    public 
#endif
    struct Rect : IFormattable
    {
        public float _x;
        public float _y;
        public float _width;
        public float _height;

        private const double EmptyX = double.PositiveInfinity;
        private const double EmptyY = double.PositiveInfinity;
        private const double EmptyWidth = double.NegativeInfinity;
        private const double EmptyHeight = double.NegativeInfinity;

        private static readonly Rect s_empty = CreateEmptyRect();

        public Rect(float x,
                    float y,
                    float width,
                    float height)
        {
            if (width < 0)
                throw new ArgumentOutOfRangeException(nameof(width), GSR.ArgumentOutOfRange_NeedNonNegNum);
            if (height < 0)
                throw new ArgumentOutOfRangeException(nameof(height), GSR.ArgumentOutOfRange_NeedNonNegNum);

            _x = x;
            _y = y;
            _width = width;
            _height = height;
        }

        public Rect(double x,
                    double y,
                    double width,
                    double height) : this((float)x, (float)y, (float)width, (float)height) { }

        public Rect(Point point1,
                    Point point2)
        {
            _x = Math.Min(point1._x, point2._x);
            _y = Math.Min(point1._y, point2._y);

            _width = Math.Max(Math.Max(point1._x, point2._x) - _x, 0f);
            _height = Math.Max(Math.Max(point1._y, point2._y) - _y, 0f);
        }

        public Rect(Point location, Size size)
        {
            if (size.IsEmpty)
            {
                this = s_empty;
            }
            else
            {
                _x = location._x;
                _y = location._y;
                _width = size._width;
                _height = size._height;
            }
        }

        public double X
        {
            get { return _x; }
            set { _x = (float)value; }
        }

        public double Y
        {
            get { return _y; }
            set { _y = (float)value; }
        }

        public double Width
        {
            get { return _width; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(Width), GSR.ArgumentOutOfRange_NeedNonNegNum);

                _width = (float)value;
            }
        }

        public double Height
        {
            get { return _height; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(Height), GSR.ArgumentOutOfRange_NeedNonNegNum);

                _height = (float)value;
            }
        }

        public double Left
        {
            get { return _x; }
        }

        public double Top
        {
            get { return _y; }
        }

        public double Right
        {
            get
            {
                if (IsEmpty)
                {
                    return double.NegativeInfinity;
                }

                return _x + _width;
            }
        }

        public double Bottom
        {
            get
            {
                if (IsEmpty)
                {
                    return double.NegativeInfinity;
                }

                return _y + _height;
            }
        }

        public static Rect Empty
        {
            get { return s_empty; }
        }

        public bool IsEmpty
        {
            get { return _width < 0; }
        }

        public bool Contains(Point point)
        {
            return ContainsInternal(point._x, point._y);
        }

        public void Intersect(Rect rect)
        {
            if (!this.IntersectsWith(rect))
            {
                this = s_empty;
            }
            else
            {
                double left = Math.Max(X, rect.X);
                double top = Math.Max(Y, rect.Y);

                //  Max with 0 to prevent double weirdness from causing us to be (-epsilon..0)
                Width = Math.Max(Math.Min(X + Width, rect.X + rect.Width) - left, 0);
                Height = Math.Max(Math.Min(Y + Height, rect.Y + rect.Height) - top, 0);

                X = left;
                Y = top;
            }
        }

        public void Union(Rect rect)
        {
            if (IsEmpty)
            {
                this = rect;
            }
            else if (!rect.IsEmpty)
            {
                double left = Math.Min(Left, rect.Left);
                double top = Math.Min(Top, rect.Top);


                // We need this check so that the math does not result in NaN
                if ((rect.Width == double.PositiveInfinity) || (Width == double.PositiveInfinity))
                {
                    Width = double.PositiveInfinity;
                }
                else
                {
                    //  Max with 0 to prevent double weirdness from causing us to be (-epsilon..0)
                    double maxRight = Math.Max(Right, rect.Right);
                    Width = Math.Max(maxRight - left, 0);
                }

                // We need this check so that the math does not result in NaN
                if ((rect.Height == double.PositiveInfinity) || (Height == double.PositiveInfinity))
                {
                    Height = double.PositiveInfinity;
                }
                else
                {
                    //  Max with 0 to prevent double weirdness from causing us to be (-epsilon..0)
                    double maxBottom = Math.Max(Bottom, rect.Bottom);
                    Height = Math.Max(maxBottom - top, 0);
                }

                X = left;
                Y = top;
            }
        }

        public void Union(Point point)
        {
            Union(new Rect(point, point));
        }

        private bool ContainsInternal(float x, float y)
        {
            return ((x >= _x) && (x - _width <= _x) &&
                    (y >= _y) && (y - _height <= _y));
        }

        internal bool IntersectsWith(Rect rect)
        {
            if (_width < 0 || rect._width < 0)
            {
                return false;
            }

            return (rect._x <= _x + _width) &&
                   (rect._x + rect._width >= _x) &&
                   (rect._y <= _y + _height) &&
                   (rect._y + rect._height >= _y);
        }

        private static Rect CreateEmptyRect()
        {
            Rect rect = default;

            rect._x = (float)EmptyX;
            rect._y = (float)EmptyY;

            // the width and height properties prevent assignment of
            // negative numbers so assign directly to the backing fields.
            rect._width = (float)EmptyWidth;
            rect._height = (float)EmptyHeight;

            return rect;
        }

        public override string ToString()
        {
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(null /* format string */, null /* format provider */);
        }

        public string ToString(IFormatProvider provider)
        {
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(null /* format string */, provider);
        }

        string IFormattable.ToString(string format, IFormatProvider provider)
        {
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(format, provider);
        }


        internal string ConvertToString(string format, IFormatProvider provider)
        {
            if (IsEmpty)
            {
                return "Empty.";
            }

            // Helper to get the numeric list separator for a given culture.
            char separator = TokenizerHelper.GetNumericListSeparator(provider);
            return string.Format(provider,
                                 "{1:" + format + "}{0}{2:" + format + "}{0}{3:" + format + "}{0}{4:" + format + "}",
                                 separator,
                                 _x,
                                 _y,
                                 _width,
                                 _height);
        }

        public bool Equals(Rect value)
        {
            return (this == value);
        }

        public static bool operator ==(Rect rect1, Rect rect2)
        {
            return rect1._x == rect2._x &&
                   rect1._y == rect2._y &&
                   rect1._width == rect2._width &&
                   rect1._height == rect2._height;
        }

        public static bool operator !=(Rect rect1, Rect rect2)
        {
            return !(rect1 == rect2);
        }

        public override bool Equals(object o)
        {
            return o is Rect && this == (Rect)o;
        }

        public override int GetHashCode()
        {
            // Perform field-by-field XOR of HashCodes
            return X.GetHashCode() ^
                   Y.GetHashCode() ^
                   Width.GetHashCode() ^
                   Height.GetHashCode();
        }
    }

    [global::WinRT.WindowsRuntimeType("Windows.Foundation.FoundationContract")]
    [global::WinRT.WindowsRuntimeHelperType(typeof(global::ABI.Windows.Foundation.Size))]
#if NET
    [global::WinRT.WinRTExposedType(typeof(global::WinRT.StructTypeDetails<Size, Size>))]
#endif
    [StructLayout(LayoutKind.Sequential)]
#if EMBED
    internal
#else
    public 
#endif
    struct Size
    {
        public float _width;
        public float _height;

        private static readonly Size s_empty = CreateEmptySize();

        public Size(float width, float height)
        {
            if (width < 0)
                throw new ArgumentOutOfRangeException(nameof(width), GSR.ArgumentOutOfRange_NeedNonNegNum);
            if (height < 0)
                throw new ArgumentOutOfRangeException(nameof(height), GSR.ArgumentOutOfRange_NeedNonNegNum);
            _width = width;
            _height = height;
        }

        public Size(double width, double height) : this((float)width, (float)height) { }

        public double Width
        {
            get { return _width; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(Width), GSR.ArgumentOutOfRange_NeedNonNegNum);

                _width = (float)value;
            }
        }

        public double Height
        {
            get { return _height; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(Height), GSR.ArgumentOutOfRange_NeedNonNegNum);

                _height = (float)value;
            }
        }

        public static Size Empty
        {
            get { return s_empty; }
        }


        public bool IsEmpty
        {
            get { return Width < 0; }
        }

        private static Size CreateEmptySize()
        {
            Size size = default;
            // We can't set these via the property setters because negatives widths
            // are rejected in those APIs.
            size._width = float.NegativeInfinity;
            size._height = float.NegativeInfinity;
            return size;
        }

        public static bool operator ==(Size size1, Size size2)
        {
            return size1._width == size2._width &&
                   size1._height == size2._height;
        }

        public static bool operator !=(Size size1, Size size2)
        {
            return !(size1 == size2);
        }

        public override bool Equals(object o)
        {
            return o is Size && Size.Equals(this, (Size)o);
        }

        public bool Equals(Size value)
        {
            return Size.Equals(this, value);
        }

        public override int GetHashCode()
        {
            if (IsEmpty)
            {
                return 0;
            }
            else
            {
                // Perform field-by-field XOR of HashCodes
                return Width.GetHashCode() ^
                       Height.GetHashCode();
            }
        }

        private static bool Equals(Size size1, Size size2)
        {
            if (size1.IsEmpty)
            {
                return size2.IsEmpty;
            }
            else
            {
                return size1._width.Equals(size2._width) &&
                       size1._height.Equals(size2._height);
            }
        }

        public override string ToString()
        {
            if (IsEmpty)
            {
                return "Empty";
            }

            return string.Format("{0},{1}", _width, _height);
        }
    }

#if EMBED
    internal
#else
    public 
#endif
    static class TokenizerHelper
    {
        public static char GetNumericListSeparator(IFormatProvider provider)
        {
            char numericSeparator = ',';

            // Get the NumberFormatInfo out of the provider, if possible
            // If the IFormatProvider doesn't not contain a NumberFormatInfo, then
            // this method returns the current culture's NumberFormatInfo.
            NumberFormatInfo numberFormat = NumberFormatInfo.GetInstance(provider);

            // Is the decimal separator is the same as the list separator?
            // If so, we use the ";".
            if ((numberFormat.NumberDecimalSeparator.Length > 0) && (numericSeparator == numberFormat.NumberDecimalSeparator[0]))
            {
                numericSeparator = ';';
            }

            return numericSeparator;
        }
    }
}

namespace ABI.Windows.Foundation
{
#if EMBED
    internal
#else
    public 
#endif
    static class Point
    {
        public static string GetGuidSignature()
        {
            return "struct(Windows.Foundation.Point;f4;f4)";
        }
    }

#if EMBED
    internal
#else
    public 
#endif
    static class Rect
    {
        public static string GetGuidSignature()
        {
            return "struct(Windows.Foundation.Rect;f4;f4;f4;f4)";
        }
    }

#if EMBED
    internal
#else
    public 
#endif
    static class Size
    {
        public static string GetGuidSignature()
        {
            return "struct(Windows.Foundation.Size;f4;f4)";
        }
    }
}

namespace System.Numerics
{
#if EMBED
    internal
#else
    public 
#endif
    static class VectorExtensions
    {
        public static global::Windows.Foundation.Point ToPoint(this Vector2 vector)
        {
            return new global::Windows.Foundation.Point(vector.X, vector.Y);
        }

        public static global::Windows.Foundation.Size ToSize(this Vector2 vector)
        {
            return new global::Windows.Foundation.Size(vector.X, vector.Y);
        }

        public static Vector2 ToVector2(this global::Windows.Foundation.Point point)
        {
            return new Vector2((float)point.X, (float)point.Y);
        }

        public static Vector2 ToVector2(this global::Windows.Foundation.Size size)
        {
            return new Vector2((float)size.Width, (float)size.Height);
        }
    }
}