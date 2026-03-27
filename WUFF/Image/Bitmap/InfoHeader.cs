using WUFF.Bytes;
using WUFF.Err;

namespace WUFF.Image.Bitmap
{
    /// <summary>
    /// Describes the details of the bitmap file. Most of the information used
    /// to create this class comes from 'Inside Windows File Formats' by Tom Swan
    /// and BMP Suite.
    /// </summary>
    /// <param name="type">The header type, which is determined by the header size in bytes.</param>
    internal class InfoHeader(InfoHeader.HeaderType type)
    {
        /// <summary>
        /// The header type, which is determined by the header size in bytes.
        /// </summary>
        private readonly HeaderType _type = type;

        /// <summary>
        /// The width of the bitmap in pixels.
        /// </summary>
        private int _width;

        /// <summary>
        /// The height of the bitmap in pixels.
        /// </summary>
        private int _height;

        /// <summary>
        /// Number of colour planes. Should be 1 as bitmaps are single colour plane images.
        /// </summary>
        private ushort _planes;

        /// <summary>
        /// The number of bits used to represent each pixel. The colour depth.
        /// Usually 1, 4, 8, or 24.
        /// <list type="bullet">
        /// <item>If 1, then the bitmap is monochromatic, two colours in the colour palette. Not necessarily black and white.</item>
        /// <item>If 2, then the bitmap will use at most four colours in the colour palette. Not common, but supposedly Win CE allows it.</item>
        /// <item>If 4, then the bitmap will use at most sixteen colours in the colour palette.</item>
        /// <item>If 8, then the bitmap will use at most 256 colours in the colour palette.</item>
        /// <item>If 24, then the bitmap is true colour. Does not use a palette (though supposedly can).</item>
        /// </list>
        /// </summary>
        private ColourDepth _bitsPerPixel;

        /// <summary>
        /// Compression type.
        /// <list type="bullet">
        /// <item>0 for uncompressed.</item>
        /// <item>1 for RLE 8bpp</item>
        /// <item>2 for RLE 4bpp</item>
        /// </list>
        /// </summary>
        private Compression.Type _compressionType;

        /// <summary>
        /// Bitmap's image size in bytes. Maybe 0 if no compression.
        /// </summary>
        private uint _size;

        /// <summary>
        /// Bitmap's prefered horizontal resolution in pixels per meter.
        /// Not really used, supposedly.
        /// </summary>
        private int _horizontalRes;

        /// <summary>
        /// Bitmap's prefered vertical resolution in pixels per meter.
        /// Not really used, supposedly.
        /// </summary>
        private int _verticalRes;

        /// <summary>
        /// The number of colours in the bitmap's palette. If zero, it will mean it uses 
        /// the maximum size of the given bits per pixels.
        /// </summary>
        private uint _coloursUsed;

        /// <summary>
        /// The number of significant colours in the image. For a value of 'n', the first 
        /// 'n' colours in the palette are considered important and should be displayed 
        /// as closely as given as possible. Colours after these 'n' colours could be 
        /// dropped or a similar colour could be used. For colour depths without the 
        /// palette (16, 24, and 32), this value is meaningless. Currently, this value is
        /// ignored and only internal.
        /// </summary>
        private uint _importantColours;

        // TODO comment and implement
        private uint _red_mask;
        private uint _green_mask;
        private uint _blue_mask;
        private uint _alpha_mask;

        //
        // PROPERTIES
        //////////////

        /// <summary>
        /// The number of colours in the palette. Will be 0 if no palette is used.
        /// </summary>
        public uint ColoursInPalette => (uint)_bitsPerPixel <= 8 ? _coloursUsed == 0 ? (uint)1 << (int)_bitsPerPixel : _coloursUsed : 0;

        /// <summary>
        /// The compression the bitmap file used to store the image data. See <see cref="Compression"/> for more information.
        /// </summary>
        public Compression.Type CompressionUsed { get => _compressionType; }

        /// <summary>
        /// The colour depth of the bitmap. See <see cref="ColourDepth"/> for more information.
        /// </summary>
        public ColourDepth Depth { get => _bitsPerPixel; }

        /// <summary>
        /// The height of the bitmap in pixels.
        /// </summary>
        public int Height { get => Math.Abs(_height); }

        /// <summary>
        /// If the height is negative in the file, the rows should be considered 
        /// top to bottom instead of the standard bottom to top.
        /// </summary>
        internal bool IsMirroredVertically => _height < 0;

        /// <summary>
        /// The size of the image in pixels.
        /// </summary>
        public int Size => Width * Height;

        /// <summary>
        /// The header type, which is determined by the header size in bytes.
        /// </summary>
        internal HeaderType Type { get { return _type; } }

        /// <summary>
        /// The width of the bitmap in pixels.
        /// </summary>
        public int Width { get => _width; }

        /// <summary>
        /// Parse the provided bytes, starting at the given offset, as if it were a bitmap infomation header.
        /// </summary>
        /// <param name="bytes">The bytes to parse.</param>
        /// <param name="offset">The starting point to parse at.</param>
        /// <returns>A <see cref="InfoHeader"/> that contains the info header details.</returns>
        /// <exception cref="FileParseException">Should any issues related to parsing the file occur.</exception>
        public static InfoHeader Parse(Span<byte> bytes, uint offset = 0)
        {
            LittleEndianReader reader = new(bytes, offset);
            InfoHeader info = new((HeaderType)reader.UInt());

            if (offset + (uint)info._type > bytes.Length)
            {
                throw new FileParseException("Invalid bitmap file size. Not enough bytes for the info header.");
            }

            switch (info._type)
            {
                case HeaderType.CoreHeader:
                    ParseCoreHeader(reader, info);
                    break;

                case HeaderType.WindowsHeaderV5:
                    ParseWindowsHeaderV5(reader, info);
                    break;

                case HeaderType.WindowsHeaderV4:
                    ParseWindowsHeaderV4(reader, info);
                    break;

                case HeaderType.WindowsHeaderV3:
                    ParseWindowsHeaderV3(reader, info);
                    break;

                case HeaderType.WindowsHeaderV2:
                    ParseWindowsHeaderV2(reader, info);
                    break;

                case HeaderType.WindowsHeaderV1:
                    ParseWindowsHeaderV1(reader, info);
                    break;

                default:
                    throw new FileParseException("Bitmap info header type not recognized: " + info._type);
            }

            // DETECT ERRORS

            // Width cannot be negative, though height can be.
            FileParseException.ThrowIfNegative(info._width, "Width cannot be negative.");

            // RLE8 and RLE4 can only be used with 8-bit and 4-bit bitmaps respectively.
            if (info.CompressionUsed == Compression.Type.RLE8) 
                FileParseException.ThrowIfNotEqual(info.Depth, ColourDepth.VGA, "Detected RLE8 when colour depth is not 8-bits.");

            if (info.CompressionUsed == Compression.Type.RLE4) 
                FileParseException.ThrowIfNotEqual(info.Depth, ColourDepth.EGA, "Detected RLE4 when colour depth is not 4-bits.");

            return info;
        }

        /// <summary>
        /// Parses the core header, the orginial DIB BMP header "BITMAPCOREHEADER".
        /// </summary>
        /// <param name="reader">The reader to pull the bytes.</param>
        /// <param name="info">The info header to fill with the parsed information.</param>
        private static void ParseCoreHeader(LittleEndianReader reader, InfoHeader info)
        {
            reader.Position = 4;
            info._width = reader.Short();
            info._height = reader.Short();
            info._planes = reader.UShort();
            info._bitsPerPixel = (ColourDepth)reader.UShort();
        }

        /// <summary>
        /// Parse the standard bmp header "BITMAPINFOHEADER". 
        /// </summary>
        /// <param name="reader">The reader to pull the bytes.</param>
        /// <param name="info">The info header to fill with the parsed information.</param>
        private static void ParseWindowsHeaderV1(LittleEndianReader reader, InfoHeader info)
        {
            reader.Position = 4;
            info._width = reader.Int();
            info._height = reader.Int();
            info._planes = reader.UShort();
            info._bitsPerPixel = (ColourDepth)reader.UShort();
            info._compressionType = (Compression.Type)reader.UInt();
            info._size = reader.UInt();
            info._horizontalRes = reader.Int();
            info._verticalRes = reader.Int();
            info._coloursUsed = reader.UInt();
            info._importantColours = reader.UInt();
        }

        /// <summary>
        /// Parse the bmp header "BITMAPV2INFOHEADER". 
        /// </summary>
        /// <param name="reader">The reader to pull the bytes.</param>
        /// <param name="info">The info header to fill with the parsed information.</param>
        private static void ParseWindowsHeaderV2(LittleEndianReader reader, InfoHeader info)
        {
            ParseWindowsHeaderV1(reader, info);
            reader.Position = 40;
            info._red_mask = reader.UInt();
            info._green_mask = reader.UInt();
            info._blue_mask = reader.UInt();
        }

        /// <summary>
        /// Parse the bmp header "BITMAPV3INFOHEADER". 
        /// </summary>
        /// <param name="reader">The reader to pull the bytes.</param>
        /// <param name="info">The info header to fill with the parsed information.</param>
        private static void ParseWindowsHeaderV3(LittleEndianReader reader, InfoHeader info)
        {
            ParseWindowsHeaderV2(reader, info);
            reader.Position = 52;
            info._alpha_mask = reader.UInt();
        }

        /// <summary>
        /// Parse the bmp header "BITMAPV4HEADER". 
        /// </summary>
        /// <param name="reader">The reader to pull the bytes.</param>
        /// <param name="info">The info header to fill with the parsed information.</param>
        private static void ParseWindowsHeaderV4(LittleEndianReader reader, InfoHeader info)
        {
            ParseWindowsHeaderV3(reader, info);
            reader.Position = 56;
            //TODO
            throw new NotImplementedException("Info Header v4 is not yet supported.");
        }

        /// <summary>
        /// Parse the bmp header "BITMAPV5HEADER". 
        /// </summary>
        /// <param name="reader">The reader to pull the bytes.</param>
        /// <param name="info">The info header to fill with the parsed information.</param>
        private static void ParseWindowsHeaderV5(LittleEndianReader reader, InfoHeader info)
        {
            ParseWindowsHeaderV4(reader, info);
            reader.Position = 108;
            //TODO
            throw new NotImplementedException("Info Header v5 is not yet supported.");
        }

        /// <summary>
        /// The header type, which as an integer, represents 
        /// the header's size in bytes.
        /// </summary>
        internal enum HeaderType
        {
            /// <summary>
            /// Windows 2.x bitmap header. Contains only basic information
            /// and the width and height are only 2 bytes.
            /// </summary>
            CoreHeader = 12,

            /// <summary>
            /// Windows 3.x bitmap header. Same structure as 
            /// <see cref="CoreHeader"/>, but changes width and height 
            /// to 4-byte integers and adds several more details. Seems
            /// to be the most common header.
            /// </summary>
            WindowsHeaderV1 = 40,

            /// <summary>
            /// Extends the <see cref="WindowsHeaderV1"/> by adding
            /// RGB bit masks.
            /// </summary>
            WindowsHeaderV2 = 52,

            /// <summary>
            /// Extends the <see cref="WindowsHeaderV2"/> by adding 
            /// an alpha mask.
            /// </summary>
            WindowsHeaderV3 = 56,

            /// <summary>
            /// Extends the <see cref="WindowsHeaderV3"/> by adding 
            /// color space  type and gamma correction.
            /// </summary>
            WindowsHeaderV4 = 108,

            /// <summary>
            /// Extends the <see cref="WindowsHeaderV4"/> by adding
            /// ICC color profiles.
            /// </summary>
            WindowsHeaderV5 = 124
        }
    }
}
