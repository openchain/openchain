// Copyright 2015 Coinprism, Inc.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace Openchain
{
    /// <summary>
    /// Represents an immutable string of binary data.
    /// </summary>
    public class ByteString : IEquatable<ByteString>
    {
        private readonly byte[] data;

        static ByteString()
        {
            Empty = new ByteString(new byte[0]);
        }

        /// <summary>
        /// Initializes a new instance of the <c <see cref="ByteString"/> class from a collection of bytes.
        /// </summary>
        /// <param name="data">An enumeration of bytes used to initialize the instance.</param>
        public ByteString(IEnumerable<byte> data)
        {
            this.data = data.ToArray();
            this.Value = new ReadOnlyCollection<byte>(this.data);
        }

        /// <summary>
        /// Initializes a new instance of the <c <see cref="ByteString"/> class from a byte array.
        /// </summary>
        /// <param name="data">The byte array used to initialize the instance</param>
        public ByteString(byte[] data)
        {
            this.data = new byte[data.Length];
            Buffer.BlockCopy(data, 0, this.data, 0, data.Length);
            this.Value = new ReadOnlyCollection<byte>(this.data);
        }

        /// <summary>
        /// Gets an empty <see cref="ByteString"/>.
        /// </summary>
        public static ByteString Empty { get; }

        /// <summary>
        /// Gets a read-only collection containing all the bytes in the <see cref="ByteString"/>.
        /// </summary>
        public IReadOnlyList<byte> Value { get; }

        /// <summary>
        /// Parses a <see cref="ByteString"/> from a hexadecimal string.
        /// </summary>
        /// <param name="hexValue">The hexadecimal string to parse.</param>
        /// <returns></returns>
        public static ByteString Parse(string hexValue)
        {
            if (hexValue == null)
                throw new FormatException("The hexValue parameter must not be null.");

            if (hexValue.Length % 2 == 1)
                throw new FormatException("The hexValue parameter must have an even number of digits.");

            byte[] result = new byte[hexValue.Length >> 1];

            for (int i = 0; i < hexValue.Length >> 1; ++i)
                result[i] = (byte)((GetHexValue(hexValue[i << 1]) << 4) + (GetHexValue(hexValue[(i << 1) + 1])));

            return new ByteString(result);
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
        /// Copies the <see cref="ByteString"/> to a buffer.
        /// </summary>
        /// <param name="buffer">The destination buffer.</param>
        /// <param name="index">The index in the buffer to which to copy this <see cref="ByteString"/>.</param>
        public void CopyTo(byte[] buffer, int index)
        {
            Buffer.BlockCopy(data, 0, buffer, index, data.Length);
        }

        /// <summary>
        /// Returns a read-only stream containing the data represented by the current object.
        /// </summary>
        /// <returns></returns>
        public Stream ToStream()
        {
            return new MemoryStream(this.data, 0, this.data.Length, false);
        }

        internal Google.Protobuf.ByteString ToProtocolBuffers()
        {
            return Google.Protobuf.ByteString.Unsafe.FromBytes(this.data);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="other">The object to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
        public bool Equals(ByteString other)
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
            if (obj is ByteString)
                return this.Equals((ByteString)obj);
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
