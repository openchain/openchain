using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace OpenChain.Core
{
    public class BinaryData : IEquatable<BinaryData>
    {
        private readonly byte[] data;

        static BinaryData()
        {
            Empty = new BinaryData(new byte[0]);
        }

        public BinaryData(IEnumerable<byte> data)
        {
            this.data = data.ToArray();
            this.Value = new ReadOnlyCollection<byte>(this.data);
        }

        public BinaryData(byte[] data)
        {
            this.data = new byte[data.Length];
            Buffer.BlockCopy(data, 0, this.data, 0, data.Length);
            this.Value = new ReadOnlyCollection<byte>(this.data);
        }

        public static BinaryData Empty { get; }

        public IReadOnlyList<byte> Value { get; }

        public static BinaryData Parse(string hexValue)
        {
            if (hexValue == null)
                throw new FormatException("The hexValue parameter must not be null.");

            if (hexValue.Length % 2 == 1)
                throw new FormatException("The hexValue parameter must have an even number of digits.");

            byte[] result = new byte[hexValue.Length >> 1];

            for (int i = 0; i < hexValue.Length >> 1; ++i)
                result[i] = (byte)((GetHexValue(hexValue[i << 1]) << 4) + (GetHexValue(hexValue[(i << 1) + 1])));

            return new BinaryData(result);
        }

        private static int GetHexValue(char hex)
        {
            int value = "0123456789ABCDEF".IndexOf(char.ToUpper(hex));

            if (value < 0)
                throw new FormatException(string.Format("The character '{0}' is not a hexadecimal digit.", hex));
            else
                return value;
        }

        public byte[] ToByteArray()
        {
            byte[] result = new byte[data.Length];
            Buffer.BlockCopy(data, 0, result, 0, data.Length);
            return result;
        }

        public bool Equals(BinaryData other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                if (this.data.Length != other.data.Length)
                    return false;

                for (int i = 0; i < other.data.Length; i++)
                    if (this.data[i] != other.data[i])
                        return false;

                return true;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is BinaryData)
                return this.Equals((BinaryData)obj);
            else
                return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = 0;
                foreach (byte b in this.data)
                    result = (result * 31) ^ b;

                return result;
            }
        }

        public override string ToString()
        {
            StringBuilder hex = new StringBuilder(this.data.Length * 2);

            foreach (byte value in this.data)
                hex.AppendFormat("{0:x2}", value);

            return hex.ToString();
        }
    }
}
