using static System.Buffers.Binary.BinaryPrimitives;

namespace WUFF.Bytes
{
    /// <summary>
    /// Reader that reads in values from a byte array using little endian.
    /// </summary>
    internal class LittleEndianReader
    {
        /// <summary>
        /// Bytes to read from.
        /// </summary>
        private readonly byte[] _bytes;

        /// <summary>
        /// The offset from the start of the bytes to consider index 0.
        /// </summary>
        private int _offset;

        /// <summary>
        /// Current position in <see cref="_bytes"/>.
        /// </summary>
        private int _position;

        /// <summary>
        /// Whether the reader is at the end of the byte array or not.
        /// </summary>
        public bool HasMore => _position < _bytes.Length - _offset;

        /// <summary>
        /// The current position of the reader in the byte array.
        /// </summary>
        public int Position
        {
            get => _position;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(value, _bytes.Length - _offset);
                _position = value;
            }
        }

        /// <summary>
        /// Get how many bytes are left in the reader.
        /// </summary>
        public int Remaining => Math.Max(_bytes.Length - _offset - _position, 0);

        /// <summary>
        /// Create a reader over the given bytes.
        /// </summary>
        /// <param name="bytes">The bytes the reader will read.</param>
        /// <param name="offset">The starting point in the given bytes for the reader to consider index 0.</param>
        public LittleEndianReader(byte[] bytes, uint offset = 0)
        {
            ArgumentOutOfRangeException.ThrowIfNegative((int)offset);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(offset, (uint)bytes.Length);

            _offset = (int)offset;
            _bytes = bytes;
            _position = 0;
        }

        /// <summary>
        /// Read an unsigned byte (1-byte unsigned integer).
        /// </summary>
        /// <returns>An unsigned byte.</returns>
        public byte Byte()
        {
            ValidateOperation(1);
            byte value = _bytes[_position + _offset];
            _position += 1;
            return value;
        }

        /// <summary>
        /// Read a signed int (4-byte signed integer).
        /// </summary>
        /// <returns>A signed int.</returns>
        public int Int()
        {
            ValidateOperation(4);
            int value = ReadInt32LittleEndian(_bytes.AsSpan()[(_position + _offset)..]);
            _position += 4;
            return value;
        }

        /// <summary>
        /// Read a signed long (8-byte signed integer).
        /// </summary>
        /// <returns>A signed long.</returns>
        public long Long()
        {
            ValidateOperation(8);
            long value = ReadInt64LittleEndian(_bytes.AsSpan()[(_position + _offset)..]);
            _position += 8;
            return value;
        }

        /// <summary>
        /// Read a signed byte (1-byte signed integer).
        /// </summary>
        /// <returns>A signed byte.</returns>
        public sbyte SByte()
        {
            ValidateOperation(1);
            sbyte value = (sbyte)_bytes[_position + _offset];
            _position += 1;
            return value;
        }

        /// <summary>
        /// Read a signed short (2-byte signed integer).
        /// </summary>
        /// <returns>A signed short.</returns>
        public short Short()
        {
            ValidateOperation(2);
            short value = ReadInt16LittleEndian(_bytes.AsSpan()[(_position + _offset)..]);
            _position += 2;
            return value;
        }

        /// <summary>
        /// Read an unsigned int (4-byte unsigned integer).
        /// </summary>
        /// <returns>An unsigned int.</returns>
        public uint UInt()
        {
            ValidateOperation(4);
            uint value = ReadUInt32LittleEndian(_bytes.AsSpan()[(_position + _offset)..]);
            _position += 4;
            return value;
        }

        /// <summary>
        /// Read an unsigned long (8-byte unsigned integer).
        /// </summary>
        /// <returns>An unsigned long.</returns>
        public ulong ULong()
        {
            ValidateOperation(8);
            ulong value = ReadUInt64LittleEndian(_bytes.AsSpan()[(_position + _offset)..]);
            _position += 8;
            return value;
        }

        /// <summary>
        /// Read an unsigned short (2-byte unsigned integer).
        /// </summary>
        /// <returns>An unsigned short.</returns>
        public ushort UShort()
        {
            ValidateOperation(2);
            ushort value = ReadUInt16LittleEndian(_bytes.AsSpan()[(_position + _offset)..]);
            _position += 2;
            return value;
        }

        /// <summary>
        /// Validates whether the operation can be performed or not based on how 
        /// many bytes are remaining in array in relation to the current position.
        /// </summary>
        /// <param name="bytesRequired">
        /// The number of bytes that are required to perform the operation.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Should there not be enough bytes remaining in the array to perform the operation.
        /// </exception>
        private void ValidateOperation(uint bytesRequired)
        {
            if (_position + bytesRequired > _bytes.Length)
            {
                throw new InvalidOperationException(
                    "Not enought bytes to perform read: pos = " + _position
                    + ", length = " + (_position + _offset)
                    + ", bytes = " + bytesRequired
                );
            }
        }
    }
}
