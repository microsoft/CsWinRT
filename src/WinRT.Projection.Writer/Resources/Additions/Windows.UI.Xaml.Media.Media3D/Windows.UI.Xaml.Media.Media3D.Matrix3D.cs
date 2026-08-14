
namespace Windows.UI.Xaml.Media.Media3D
{
    using global::Windows.Foundation;

#if !CSWINRT_REFERENCE_PROJECTION
    [WindowsRuntimeType]
    [WindowsRuntimeClassName("Windows.Foundation.IReference`1<Windows.UI.Xaml.Media.Media3D.Matrix3D>")]
    [ABI.Windows.UI.Xaml.Media.Media3D.Matrix3DComWrappersMarshaller]
#endif
    public struct Matrix3D : IFormattable, IEquatable<Matrix3D>
    {
        public double M11;
        public double M12;
        public double M13;
        public double M14;
        public double M21;
        public double M22;
        public double M23;
        public double M24;
        public double M31;
        public double M32;
        public double M33;
        public double M34;
        public double OffsetX;
        public double OffsetY;
        public double OffsetZ;
        public double M44;

        // Assuming this matrix has fourth column of 0,0,0,1 and isn't identity this function:
        // Returns false if HasInverse is false, otherwise inverts the matrix.
        private bool NormalizedAffineInvert()
        {
            double z20 = M12 * M23 - M22 * M13;
            double z10 = M32 * M13 - M12 * M33;
            double z00 = M22 * M33 - M32 * M23;
            double det = M31 * z20 + M21 * z10 + M11 * z00;

            if (IsZero(det))
            {
                return false;
            }

            // Compute 3x3 non-zero cofactors for the 2nd column
            double z21 = M21 * M13 - M11 * M23;
            double z11 = M11 * M33 - M31 * M13;
            double z01 = M31 * M23 - M21 * M33;

            // Compute all six 2x2 determinants of 1st two columns
            double y01 = M11 * M22 - M21 * M12;
            double y02 = M11 * M32 - M31 * M12;
            double y03 = M11 * OffsetY - OffsetX * M12;
            double y12 = M21 * M32 - M31 * M22;
            double y13 = M21 * OffsetY - OffsetX * M22;
            double y23 = M31 * OffsetY - OffsetX * M32;

            // Compute all non-zero and non-one 3x3 cofactors for 2nd
            // two columns
            double z23 = M23 * y03 - OffsetZ * y01 - M13 * y13;
            double z13 = M13 * y23 - M33 * y03 + OffsetZ * y02;
            double z03 = M33 * y13 - OffsetZ * y12 - M23 * y23;
            double z22 = y01;
            double z12 = -y02;
            double z02 = y12;

            double rcp = 1.0 / det;

            // Multiply all 3x3 cofactors by reciprocal & transpose
            M11 = (z00 * rcp);
            M12 = (z10 * rcp);
            M13 = (z20 * rcp);

            M21 = (z01 * rcp);
            M22 = (z11 * rcp);
            M23 = (z21 * rcp);

            M31 = (z02 * rcp);
            M32 = (z12 * rcp);
            M33 = (z22 * rcp);

            OffsetX = (z03 * rcp);
            OffsetY = (z13 * rcp);
            OffsetZ = (z23 * rcp);

            return true;
        }

        // RETURNS true if has inverse & invert was done.  Otherwise returns false & leaves matrix unchanged.
        private bool InvertCore()
        {
            if (IsAffine)
            {
                return NormalizedAffineInvert();
            }

            // compute all six 2x2 determinants of 2nd two columns
            double y01 = M13 * M24 - M23 * M14;
            double y02 = M13 * M34 - M33 * M14;
            double y03 = M13 * M44 - OffsetZ * M14;
            double y12 = M23 * M34 - M33 * M24;
            double y13 = M23 * M44 - OffsetZ * M24;
            double y23 = M33 * M44 - OffsetZ * M34;

            // Compute 3x3 cofactors for 1st the column
            double z30 = M22 * y02 - M32 * y01 - M12 * y12;
            double z20 = M12 * y13 - M22 * y03 + OffsetY * y01;
            double z10 = M32 * y03 - OffsetY * y02 - M12 * y23;
            double z00 = M22 * y23 - M32 * y13 + OffsetY * y12;

            // Compute 4x4 determinant
            double det = OffsetX * z30 + M31 * z20 + M21 * z10 + M11 * z00;

            if (IsZero(det))
            {
                return false;
            }

            // Compute 3x3 cofactors for the 2nd column
            double z31 = M11 * y12 - M21 * y02 + M31 * y01;
            double z21 = M21 * y03 - OffsetX * y01 - M11 * y13;
            double z11 = M11 * y23 - M31 * y03 + OffsetX * y02;
            double z01 = M31 * y13 - OffsetX * y12 - M21 * y23;

            // Compute all six 2x2 determinants of 1st two columns
            y01 = M11 * M22 - M21 * M12;
            y02 = M11 * M32 - M31 * M12;
            y03 = M11 * OffsetY - OffsetX * M12;
            y12 = M21 * M32 - M31 * M22;
            y13 = M21 * OffsetY - OffsetX * M22;
            y23 = M31 * OffsetY - OffsetX * M32;

            // Compute all 3x3 cofactors for 2nd two columns
            double z33 = M13 * y12 - M23 * y02 + M33 * y01;
            double z23 = M23 * y03 - OffsetZ * y01 - M13 * y13;
            double z13 = M13 * y23 - M33 * y03 + OffsetZ * y02;
            double z03 = M33 * y13 - OffsetZ * y12 - M23 * y23;
            double z32 = M24 * y02 - M34 * y01 - M14 * y12;
            double z22 = M14 * y13 - M24 * y03 + M44 * y01;
            double z12 = M34 * y03 - M44 * y02 - M14 * y23;
            double z02 = M24 * y23 - M34 * y13 + M44 * y12;

            double rcp = 1.0 / det;

            // Multiply all 3x3 cofactors by reciprocal & transpose
            M11 = (z00 * rcp);
            M12 = (z10 * rcp);
            M13 = (z20 * rcp);
            M14 = (z30 * rcp);

            M21 = (z01 * rcp);
            M22 = (z11 * rcp);
            M23 = (z21 * rcp);
            M24 = (z31 * rcp);

            M31 = (z02 * rcp);
            M32 = (z12 * rcp);
            M33 = (z22 * rcp);
            M34 = (z32 * rcp);

            OffsetX = (z03 * rcp);
            OffsetY = (z13 * rcp);
            OffsetZ = (z23 * rcp);
            M44 = (z33 * rcp);

            return true;
        }

        public Matrix3D(double m11, double m12, double m13, double m14,
                        double m21, double m22, double m23, double m24,
                        double m31, double m32, double m33, double m34,
                        double offsetX, double offsetY, double offsetZ, double m44)
        {
            M11 = m11;
            M12 = m12;
            M13 = m13;
            M14 = m14;
            M21 = m21;
            M22 = m22;
            M23 = m23;
            M24 = m24;
            M31 = m31;
            M32 = m32;
            M33 = m33;
            M34 = m34;
            OffsetX = offsetX;
            OffsetY = offsetY;
            OffsetZ = offsetZ;
            M44 = m44;
        }

        // the transform is identity by default
        // Actually fill in the fields - some (internal) code uses the fields directly for perf.
        private static Matrix3D s_identity = CreateIdentity();

        public static Matrix3D Identity
        {
            get
            {
                return s_identity;
            }
        }

        public readonly bool IsIdentity
        {
            get
            {
                return M11 == 1 && M12 == 0 && M13 == 0 && M14 == 0 &&
                         M21 == 0 && M22 == 1 && M23 == 0 && M24 == 0 &&
                         M31 == 0 && M32 == 0 && M33 == 1 && M34 == 0 &&
                         OffsetX == 0 && OffsetY == 0 && OffsetZ == 0 && M44 == 1;
            }
        }

        public readonly override string ToString()
        {
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(null /* format string */, null /* format provider */);
        }

        public readonly string ToString(IFormatProvider provider)
        {
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(null /* format string */, provider);
        }

        readonly string IFormattable.ToString(string format, IFormatProvider provider)
        {
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(format, provider);
        }

        private readonly string ConvertToString(string format, IFormatProvider provider)
        {
#if CSWINRT_REFERENCE_PROJECTION
            throw null;
#else
            if (IsIdentity)
            {
                return "Identity";
            }

            // Helper to get the numeric list separator for a given culture.
            char separator = global::WindowsRuntime.InteropServices.TokenizerHelper.GetNumericListSeparator(provider);
            DefaultInterpolatedStringHandler handler = new(0, 31, provider, stackalloc char[256]);
            handler.AppendFormatted(M11, format);
            handler.AppendFormatted(separator);
            handler.AppendFormatted(M12, format);
            handler.AppendFormatted(separator);
            handler.AppendFormatted(M13, format);
            handler.AppendFormatted(separator);
            handler.AppendFormatted(M14, format);
            handler.AppendFormatted(separator);
            handler.AppendFormatted(M21, format);
            handler.AppendFormatted(separator);
            handler.AppendFormatted(M22, format);
            handler.AppendFormatted(separator);
            handler.AppendFormatted(M23, format);
            handler.AppendFormatted(separator);
            handler.AppendFormatted(M24, format);
            handler.AppendFormatted(separator);
            handler.AppendFormatted(M31, format);
            handler.AppendFormatted(separator);
            handler.AppendFormatted(M32, format);
            handler.AppendFormatted(separator);
            handler.AppendFormatted(M33, format);
            handler.AppendFormatted(separator);
            handler.AppendFormatted(M34, format);
            handler.AppendFormatted(separator);
            handler.AppendFormatted(OffsetX, format);
            handler.AppendFormatted(separator);
            handler.AppendFormatted(OffsetY, format);
            handler.AppendFormatted(separator);
            handler.AppendFormatted(OffsetZ, format);
            handler.AppendFormatted(separator);
            handler.AppendFormatted(M44, format);
            return handler.ToStringAndClear();
#endif
        }

        public readonly override int GetHashCode()
        {
            // Perform field-by-field XOR of HashCodes
            return M11.GetHashCode() ^
                   M12.GetHashCode() ^
                   M13.GetHashCode() ^
                   M14.GetHashCode() ^
                   M21.GetHashCode() ^
                   M22.GetHashCode() ^
                   M23.GetHashCode() ^
                   M24.GetHashCode() ^
                   M31.GetHashCode() ^
                   M32.GetHashCode() ^
                   M33.GetHashCode() ^
                   M34.GetHashCode() ^
                   OffsetX.GetHashCode() ^
                   OffsetY.GetHashCode() ^
                   OffsetZ.GetHashCode() ^
                   M44.GetHashCode();
        }

        public readonly override bool Equals(object o)
        {
            return o is Matrix3D matrix && Equals(this, matrix);
        }

        public readonly bool Equals(Matrix3D value)
        {
            return Matrix3D.Equals(this, value);
        }

        public static bool operator ==(Matrix3D matrix1, Matrix3D matrix2)
        {
            return matrix1.M11 == matrix2.M11 &&
                   matrix1.M12 == matrix2.M12 &&
                   matrix1.M13 == matrix2.M13 &&
                   matrix1.M14 == matrix2.M14 &&
                   matrix1.M21 == matrix2.M21 &&
                   matrix1.M22 == matrix2.M22 &&
                   matrix1.M23 == matrix2.M23 &&
                   matrix1.M24 == matrix2.M24 &&
                   matrix1.M31 == matrix2.M31 &&
                   matrix1.M32 == matrix2.M32 &&
                   matrix1.M33 == matrix2.M33 &&
                   matrix1.M34 == matrix2.M34 &&
                   matrix1.OffsetX == matrix2.OffsetX &&
                   matrix1.OffsetY == matrix2.OffsetY &&
                   matrix1.OffsetZ == matrix2.OffsetZ &&
                   matrix1.M44 == matrix2.M44;
        }

        public static bool operator !=(Matrix3D matrix1, Matrix3D matrix2)
        {
            return !(matrix1 == matrix2);
        }

        public static Matrix3D operator *(Matrix3D matrix1, Matrix3D matrix2)
        {
            Matrix3D matrix3D = default;

            matrix3D.M11 = matrix1.M11 * matrix2.M11 +
                           matrix1.M12 * matrix2.M21 +
                           matrix1.M13 * matrix2.M31 +
                           matrix1.M14 * matrix2.OffsetX;
            matrix3D.M12 = matrix1.M11 * matrix2.M12 +
                           matrix1.M12 * matrix2.M22 +
                           matrix1.M13 * matrix2.M32 +
                           matrix1.M14 * matrix2.OffsetY;
            matrix3D.M13 = matrix1.M11 * matrix2.M13 +
                           matrix1.M12 * matrix2.M23 +
                           matrix1.M13 * matrix2.M33 +
                           matrix1.M14 * matrix2.OffsetZ;
            matrix3D.M14 = matrix1.M11 * matrix2.M14 +
                           matrix1.M12 * matrix2.M24 +
                           matrix1.M13 * matrix2.M34 +
                           matrix1.M14 * matrix2.M44;
            matrix3D.M21 = matrix1.M21 * matrix2.M11 +
                           matrix1.M22 * matrix2.M21 +
                           matrix1.M23 * matrix2.M31 +
                           matrix1.M24 * matrix2.OffsetX;
            matrix3D.M22 = matrix1.M21 * matrix2.M12 +
                           matrix1.M22 * matrix2.M22 +
                           matrix1.M23 * matrix2.M32 +
                           matrix1.M24 * matrix2.OffsetY;
            matrix3D.M23 = matrix1.M21 * matrix2.M13 +
                           matrix1.M22 * matrix2.M23 +
                           matrix1.M23 * matrix2.M33 +
                           matrix1.M24 * matrix2.OffsetZ;
            matrix3D.M24 = matrix1.M21 * matrix2.M14 +
                           matrix1.M22 * matrix2.M24 +
                           matrix1.M23 * matrix2.M34 +
                           matrix1.M24 * matrix2.M44;
            matrix3D.M31 = matrix1.M31 * matrix2.M11 +
                           matrix1.M32 * matrix2.M21 +
                           matrix1.M33 * matrix2.M31 +
                           matrix1.M34 * matrix2.OffsetX;
            matrix3D.M32 = matrix1.M31 * matrix2.M12 +
                           matrix1.M32 * matrix2.M22 +
                           matrix1.M33 * matrix2.M32 +
                           matrix1.M34 * matrix2.OffsetY;
            matrix3D.M33 = matrix1.M31 * matrix2.M13 +
                           matrix1.M32 * matrix2.M23 +
                           matrix1.M33 * matrix2.M33 +
                           matrix1.M34 * matrix2.OffsetZ;
            matrix3D.M34 = matrix1.M31 * matrix2.M14 +
                           matrix1.M32 * matrix2.M24 +
                           matrix1.M33 * matrix2.M34 +
                           matrix1.M34 * matrix2.M44;
            matrix3D.OffsetX = matrix1.OffsetX * matrix2.M11 +
                           matrix1.OffsetY * matrix2.M21 +
                           matrix1.OffsetZ * matrix2.M31 +
                           matrix1.M44 * matrix2.OffsetX;
            matrix3D.OffsetY = matrix1.OffsetX * matrix2.M12 +
                           matrix1.OffsetY * matrix2.M22 +
                           matrix1.OffsetZ * matrix2.M32 +
                           matrix1.M44 * matrix2.OffsetY;
            matrix3D.OffsetZ = matrix1.OffsetX * matrix2.M13 +
                           matrix1.OffsetY * matrix2.M23 +
                           matrix1.OffsetZ * matrix2.M33 +
                           matrix1.M44 * matrix2.OffsetZ;
            matrix3D.M44 = matrix1.OffsetX * matrix2.M14 +
                           matrix1.OffsetY * matrix2.M24 +
                           matrix1.OffsetZ * matrix2.M34 +
                           matrix1.M44 * matrix2.M44;

            // matrix3D._type is not set.

            return matrix3D;
        }

        public readonly bool HasInverse
        {
            get
            {
                return !IsZero(Determinant);
            }
        }

        public void Invert()
        {
            if (!InvertCore())
            {
                throw new InvalidOperationException();
            }
        }

        private static Matrix3D CreateIdentity()
        {
            Matrix3D matrix3D = default;
            matrix3D.SetMatrix(1, 0, 0, 0,
                               0, 1, 0, 0,
                               0, 0, 1, 0,
                               0, 0, 0, 1);
            return matrix3D;
        }

        private void SetMatrix(double m11, double m12, double m13, double m14,
                               double m21, double m22, double m23, double m24,
                               double m31, double m32, double m33, double m34,
                               double offsetX, double offsetY, double offsetZ, double m44)
        {
            M11 = m11;
            M12 = m12;
            M13 = m13;
            M14 = m14;
            M21 = m21;
            M22 = m22;
            M23 = m23;
            M24 = m24;
            M31 = m31;
            M32 = m32;
            M33 = m33;
            M34 = m34;
            OffsetX = offsetX;
            OffsetY = offsetY;
            OffsetZ = offsetZ;
            M44 = m44;
        }

        private static bool Equals(Matrix3D matrix1, Matrix3D matrix2)
        {
            return matrix1.M11.Equals(matrix2.M11) &&
                   matrix1.M12.Equals(matrix2.M12) &&
                   matrix1.M13.Equals(matrix2.M13) &&
                   matrix1.M14.Equals(matrix2.M14) &&
                   matrix1.M21.Equals(matrix2.M21) &&
                   matrix1.M22.Equals(matrix2.M22) &&
                   matrix1.M23.Equals(matrix2.M23) &&
                   matrix1.M24.Equals(matrix2.M24) &&
                   matrix1.M31.Equals(matrix2.M31) &&
                   matrix1.M32.Equals(matrix2.M32) &&
                   matrix1.M33.Equals(matrix2.M33) &&
                   matrix1.M34.Equals(matrix2.M34) &&
                   matrix1.OffsetX.Equals(matrix2.OffsetX) &&
                   matrix1.OffsetY.Equals(matrix2.OffsetY) &&
                   matrix1.OffsetZ.Equals(matrix2.OffsetZ) &&
                   matrix1.M44.Equals(matrix2.M44);
        }

        private readonly double GetNormalizedAffineDeterminant()
        {
            double z20 = M12 * M23 - M22 * M13;
            double z10 = M32 * M13 - M12 * M33;
            double z00 = M22 * M33 - M32 * M23;

            return M31 * z20 + M21 * z10 + M11 * z00;
        }

        private readonly bool IsAffine
        {
            get
            {
                return M14 == 0.0 && M24 == 0.0 && M34 == 0.0 && M44 == 1.0;
            }
        }

        private readonly double Determinant
        {
            get
            {
                if (IsAffine)
                {
                    return GetNormalizedAffineDeterminant();
                }

                // compute all six 2x2 determinants of 2nd two columns
                double y01 = M13 * M24 - M23 * M14;
                double y02 = M13 * M34 - M33 * M14;
                double y03 = M13 * M44 - OffsetZ * M14;
                double y12 = M23 * M34 - M33 * M24;
                double y13 = M23 * M44 - OffsetZ * M24;
                double y23 = M33 * M44 - OffsetZ * M34;

                // Compute 3x3 cofactors for 1st the column
                double z30 = M22 * y02 - M32 * y01 - M12 * y12;
                double z20 = M12 * y13 - M22 * y03 + OffsetY * y01;
                double z10 = M32 * y03 - OffsetY * y02 - M12 * y23;
                double z00 = M22 * y23 - M32 * y13 + OffsetY * y12;

                return OffsetX * z30 + M31 * z20 + M21 * z10 + M11 * z00;
            }
        }

        private static bool IsZero(double value)
        {
            return Math.Abs(value) < 10.0 * DBL_EPSILON_RELATIVE_1;
        }

        private const double DBL_EPSILON_RELATIVE_1 = 1.1102230246251567e-016; /* smallest such that 1.0+DBL_EPSILON != 1.0 */
    }
}
