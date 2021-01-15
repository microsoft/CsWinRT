using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Generator
{
    public static class Helper
    {
        public static Guid EncodeGuid(byte[] data)
        {
            if (BitConverter.IsLittleEndian)
            {
                // swap bytes of int a
                byte t = data[0];
                data[0] = data[3];
                data[3] = t;
                t = data[1];
                data[1] = data[2];
                data[2] = t;
                // swap bytes of short b
                t = data[4];
                data[4] = data[5];
                data[5] = t;
                // swap bytes of short c and encode rfc time/version field
                t = data[6];
                data[6] = data[7];
                data[7] = (byte)((t & 0x0f) | (5 << 4));
                // encode rfc clock/reserved field
                data[8] = (byte)((data[8] & 0x3f) | 0x80);
            }
            return new Guid(data.Take(16).ToArray());
        }
    }

    class AttributeDataComparer : IEqualityComparer<AttributeData>
    {
        public bool Equals(AttributeData x, AttributeData y)
        {
            return x.ToString() == y.ToString();
        }

        public int GetHashCode(AttributeData obj)
        {
            return obj.ToString().GetHashCode();
        }
    }
}
