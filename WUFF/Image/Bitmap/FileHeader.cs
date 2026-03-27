using WUFF.Err;
using WUFF.Bytes;

namespace WUFF.Image.Bitmap
{
    /// <summary>
    /// Respresents the initial bytes of a bitmap file. Provides 
    /// the file type characters 'BM', size of the file, and 
    /// offset to image data.
    /// </summary>
    internal struct FileHeader
    {
        //
        // CONTSTANTS
        //////////////

        /// <summary>
        /// The type that is expected to be found in a bitmap file. Should be 'BM'.
        /// </summary>
        public const ushort BitmapType = 0x4D42;

        /// <summary>
        /// The size of a bitmap file header in bytes.
        /// </summary>
        public const uint BitmapFileHeaderSize = 14;

        //
        // VALUES
        //////////

        /// <summary>
        /// The first two bytes of the bitmap file.
        /// </summary>
        private ushort _type;

        /// <summary>
        /// The size of the Bitmap file in bytes. This includes the headers, not just the image size.
        /// </summary>
        private uint _size;

        /// <summary>
        /// Reserved value 1. Not necessary for parsing, can be zero/ignored.
        /// </summary>
        private ushort _reserved1;

        /// <summary>
        /// Reserved value 2. Not necessary for parsing, can be zero/ignored.
        /// </summary>
        private ushort _reserved2;

        /// <summary>
        /// Offset in bytes to where the image pixel array begins, not the colour array.
        /// </summary>
        private uint _offset;

        /// <summary>
        /// Validation check to see if the type identifier and file size are correct. 
        /// The type identifier should be 'BM' and the value in the header should match 
        /// with the actual file size.
        /// </summary>
        /// <param name="fileSize">The known size of the file.</param>
        /// <returns>True if the header is valid, false otherwise.</returns>
        public readonly bool IsValid(uint fileSize) => _type == BitmapType && _size == fileSize;

        /// <summary>
        /// The offset, in bytes, to the start of the pixel array.
        /// </summary>
        public readonly uint Offset => _offset;

        /// <summary>
        /// Parse the given data into a <see cref="FileHeader"/>.
        /// </summary>
        /// <param name="data">The byte data to parse.</param>
        /// <param name="offset">The offset to the starting position of the data to parse.</param>
        /// <returns>A bitmap file header from the provided data.</returns>
        /// <exception cref="FileParseException">If the data does not have enough bytes to parse.</exception>
        public static FileHeader Parse(byte[] data, uint offset = 0) => Parse(data.AsSpan(), offset);

        /// <summary>
        /// Parse the given data into a <see cref="FileHeader"/>.
        /// </summary>
        /// <param name="data">The byte data to parse.</param>
        /// <param name="offset">The offset to the starting position of the data to parse.</param>
        /// <returns>A bitmap file header from the provided data.</returns>
        /// <exception cref="FileParseException">If the data does not have enough bytes to parse.</exception>
        public static FileHeader Parse(Span<byte> bytes, uint offset = 0)
        {
            if (offset + BitmapFileHeaderSize > bytes.Length)
            {
                throw new FileParseException("Invalid bitmap file size. Not enough bytes for the header.");
            }

            LittleEndianReader reader = new(bytes, offset);

            return new FileHeader
            {
                _type = reader.UShort(),
                _size = reader.UInt(),
                _reserved1 = reader.UShort(),
                _reserved2 = reader.UShort(),
                _offset = reader.UInt(),
            };
        }
    }
}
