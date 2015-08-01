using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Google.ProtocolBuffers;

namespace OpenChain
{
    /// <summary>
    /// Represents an immutable string of binary data.
    /// </summary>
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

        /// <summary>
        /// Gets an empty <see cref="BinaryData"/>.
        /// </summary>
        public static BinaryData Empty { get; }

        /// <summary>
        /// Gets a read-only collection containing all the bytes in the <see cref="BinaryData"/>.
        /// </summary>
        public IReadOnlyList<byte> Value { get; }

        /// <summary>
        /// Parses a <see cref="BinaryData"/> from a hexadecimal string.
        /// </summary>
        /// <param name="hexValue">The hexadecimal string to parse.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Returns a copy of the instance as an array.
        /// </summary>
        /// <returns></returns>
        public byte[] ToByteArray()
        {
            byte[] result = new byte[data.Length];
            Buffer.BlockCopy(data, 0, result, 0, data.Length);
            return result;
        }

        /// <summary>
        /// Returns a read-only stream containing the data represented by the current object.
        /// </summary>
        /// <returns></returns>
        public Stream ToStream()
        {
            return new MemoryStream(this.data, 0, this.data.Length, false, false);
        }

        internal ByteString ToByteString()
        {
            return ByteString.Unsafe.FromBytes(this.data);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="other">The object to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
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

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (obj is BinaryData)
                return this.Equals((BinaryData)obj);
            else
                return false;
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
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

        /// <summary>
        /// Returns the hexadecimal representation of the current object.
        /// </summary>
        /// <returns>The hexadecimal representation of the current object.</returns>
        public override string ToString()
        {
            StringBuilder hex = new StringBuilder(this.data.Length * 2);

            foreach (byte value in this.data)
                hex.AppendFormat("{0:x2}", value);

            return hex.ToString();
        }
    }
}
