using WUFF.Bytes;
using WUFF.Err;
using WUFF.Image.Colours;

namespace WUFF.Image.Bitmaps
{
    /// <summary>
    /// Represents a bitmap image.
    /// </summary>
    public class Bitmap : Image
    {
        /// <summary>
        /// The pixels of the bitmap image. Stored top-down, row-by-row.
        /// The first element would be the top-left pixel.
        /// </summary>
        private readonly Colour[] _pixels;

        /// <summary>
        /// Access the pixel at the position (x,y).
        /// </summary>
        /// <param name="x">The position of the pixel on the horizontal axis.</param>
        /// <param name="y">The position of the pixel on the vertical axis.</param>
        /// <returns>The pixel at the specified position.</returns>
        public Colour this[int x, int y]
        {
            get => GetPixel(x, y);
            set => SetPixel(x, y, value);
        }

        /// <summary>
        /// Initializes a bitmap give the width, height, and pixel array.
        /// </summary>
        /// <param name="width">The horizontal pixel count.</param>
        /// <param name="height">The vertical pixel count.</param>
        /// <param name="pixels">The pixels, i.e. the colours of the image.</param>
        internal Bitmap(int width, int height, Colour[] pixels) : base(width, height)
        {
            _pixels = pixels;
        }

        protected override Colour SubGetPixel(int x, int y)
        {
            return _pixels[y * Width + x];
        }

        /// <summary>
        /// Load a bitmap from the provided file.
        /// </summary>
        /// <param name="filename">The name of the file to load as a bitmap.</param>
        /// <returns>A <see cref="Bitmap"/> representing the image stored in the loaded file.</returns>
        /// <exception cref="FileParseException">Should any issues related to file parsing occur.</exception>
        public static Bitmap Load(string filename)
        {
            byte[] bytes = File.ReadAllBytes(filename);
            uint size = (uint)bytes.Length;

            uint offset = 0;
            FileHeader fileHeader = FileHeader.Parse(bytes);
            offset += FileHeader.BitmapFileHeaderSize;
            if (!fileHeader.IsValid(size)) throw new FileParseException("Size provided in header does not match actual file size.");

            // TODO add validation of headers
            InfoHeader infoHeader = InfoHeader.Parse(bytes, offset);
            offset += (uint)infoHeader.Type;

            Palette palette = Palette.Parse(bytes, infoHeader, offset);
            offset = fileHeader.Offset;

            if (offset >= bytes.Length) throw new FileParseException("Offset in file to pixel array is greater than file length.");

            if (!Enum.IsDefined(infoHeader.CompressionUsed)) throw new FileParseException("Invalid compression type found: " + infoHeader.CompressionUsed);

            LittleEndianReader reader = new(bytes, offset);

            // The size of the pixel data being stated as zero is valid.
            if (infoHeader.SizeInBytes != 0)
                FileParseException.ThrowIfNotEqual((uint)reader.Remaining, infoHeader.SizeInBytes, "Pixel data length mismatch with stated size in header.");

            Colour[] pixels = ParseColourData(bytes, infoHeader, palette, offset);

            return new Bitmap(infoHeader.Width, infoHeader.Height, pixels);
        }

        /// <summary>
        /// Parse the pixel data based on bit masks.
        /// </summary>
        /// <param name="reader">The reader to read the bytes from.</param>
        /// <param name="info">The info header to get the masks and other bitmap details from.</param>
        /// <returns>The parsed pixels.</returns>
        private static Colour[] ParseBitMask(LittleEndianReader reader, InfoHeader info)
        {
            if (info.Depth != ColourDepth.TrueColourWithAlpha && info.Depth != ColourDepth.HighColour)
                throw new FileParseException("Attempting to parse pixels with bit masks when unsupported for bitmap's colour depth.");

            Colour[] pixels = new Colour[info.Size];
            ColourMaskSet parser = info.Masks;

            foreach (int y in info.YRange)
            {
                for (int x = 0; x < info.Width; ++x)
                {
                    uint value = info.Depth == ColourDepth.HighColour ? reader.UShort() : reader.UInt();
                    pixels[y * info.Width + x] = parser.Parse(value);
                }

                if (info.Depth == ColourDepth.HighColour && (info.Width & 1) == 1) reader.UShort();
            }

            return pixels;
        }

        /// <summary>
        /// Parse the colour data for a bitmap.
        /// </summary>
        /// <param name="bytes">The bitmap data.</param>
        /// <param name="info">The bitmap info header.</param>
        /// <param name="palette">The bitmap colour header.</param>
        /// <param name="offset">The offset to where the colour data is.</param>
        /// <returns>The pixel data of the bitmap.</returns>
        /// <exception cref="FileParseException">Thrown should an issue arise when parsing the colour data.</exception>
        internal static Colour[] ParseColourData(byte[] bytes, InfoHeader info, Palette palette, uint offset = 0) => ParseColourData(new(bytes, offset), info, palette);

        /// <summary>
        /// Parse the colour data for a bitmap.
        /// </summary>
        /// <param name="reader">The reader to get the bitmap data.</param>
        /// <param name="info">The bitmap info header.</param>
        /// <param name="palette">The bitmap colour header.</param>
        /// <returns>The pixel data of the bitmap.</returns>
        /// <exception cref="FileParseException">Thrown should an issue arise when parsing the colour data.</exception>
        internal static Colour[] ParseColourData(LittleEndianReader reader, InfoHeader info, Palette palette)
        {
            if (!Enum.IsDefined(info.CompressionUsed)) throw new FileParseException("Invalid compression type found: " + info.CompressionUsed);

            // The size of the pixel data being stated as zero is valid.
            if (info.SizeInBytes != 0)
                FileParseException.ThrowIfNotEqual((uint)reader.Remaining, info.SizeInBytes, "Pixel data length mismatch with stated size in header.");

            return info.Depth switch
            {
                ColourDepth.Monochromatic => ParseWithPalette(reader, info, palette, new MonochromaticParser()),
                ColourDepth.CGA => ParseWithPalette(reader, info, palette, new CGAParser()),
                ColourDepth.EGA => info.CompressionUsed == Compression.Type.RLE4 ? Compression.RLE4Decode(reader, info, palette) : ParseWithPalette(reader, info, palette, new EGAParser()),
                ColourDepth.VGA => info.CompressionUsed == Compression.Type.RLE8 ? Compression.RLE8Decode(reader, info, palette) : ParseWithPalette(reader, info, palette),
                ColourDepth.HighColour => ParseBitMask(reader, info),
                ColourDepth.TrueColour => ParseTrueColour(reader, info),
                ColourDepth.TrueColourWithAlpha => ParseBitMask(reader, info),
                _ => throw new FileParseException("Invalid colour depth: " + info.Depth),
            };
        }

        /// <summary>
        /// Parse 24-bit per pixel bitmap.
        /// </summary>
        /// <param name="reader">The reader to parse the bytes from.</param>
        /// <param name="info">The header info.</param>
        /// <returns>The pixels of the image.</returns>
        private static Colour[] ParseTrueColour(LittleEndianReader reader, InfoHeader info)
        {
            Colour[] pixels = new Colour[info.Size];

            foreach (int y in info.YRange)
            {
                for (int x = 0; x < info.Width; ++x)
                {
                    byte blue = reader.Byte();
                    byte green = reader.Byte();
                    byte red = reader.Byte();
                    pixels[y * info.Width + x] = Colour.FromRGB(red, green, blue);
                }

                for (int i = 0; i < info.Width % 4; ++i) reader.Byte();
            }

            return pixels;
        }

        /// <summary>
        /// Parse the as though it is the image data of the bitmap, assuming that each byte is a index to a provided palette.
        /// </summary>
        /// <param name="reader">The reader to pull the data from.</param>
        /// <param name="info">The header info.</param>
        /// <param name="palette">The palette to pull the colours from.</param>
        /// <returns>The pixels of the image.</returns>
        private static Colour[] ParseWithPalette(LittleEndianReader reader, InfoHeader info, Palette palette)
        {
            Colour[] pixels = new Colour[info.Size];

            foreach (int y in info.YRange)
            {
                int offset32 = 0;

                for (int x = 0; x < info.Width; ++x)
                {
                    int index = reader.Byte();
                    pixels[y * info.Width + x] = palette[index];
                    offset32 = (offset32 + 8) & 31;
                }

                while (offset32 != 0)
                {
                    reader.Byte();
                    offset32 = (offset32 + 8) & 31;
                }
            }

            return pixels;
        }

        /// <summary>
        /// Parse the as though it is the image data of the bitmap, assuming that each byte provides indices to a provided palette.
        /// </summary>
        /// <param name="reader">The reader to pull the data from.</param>
        /// <param name="info">The header info.</param>
        /// <param name="palette">The palette to pull the colours from.</param>
        /// <param name="parser">Used to parse each byte to pull the appropriate indicies from.</param>
        /// <returns>The pixels of the image.</returns>
        private static Colour[] ParseWithPalette(LittleEndianReader reader, InfoHeader info, Palette palette, PaletteIndexParser parser)
        {
            Colour[] pixels = new Colour[info.Size];

            foreach (int y in info.YRange)
            {
                int offset32 = 0;
                bool readByte = true;
                byte currByte = 0;

                parser.NewRow();

                for (int x = 0; x < info.Width; ++x)
                {
                    int index;

                    if (readByte)
                    {
                        offset32 = (offset32 + 8) & 31;
                        currByte = reader.Byte();
                    }

                    index = parser.Parse(currByte);

                    readByte = parser.IsByteFinished();

                    pixels[y * info.Width + x] = palette[index];
                }

                while (offset32 != 0)
                {
                    reader.Byte();
                    offset32 = (offset32 + 8) % 32;
                }
            }

            return pixels;
        }

        protected override void SubSetPixel(int x, int y, Colour colour)
        {
            _pixels[y * Width + x] = colour;
        }

        /// <summary>
        /// Attempt to load the provided bitmap. If not successful,
        /// return a result giving the reason.
        /// </summary>
        /// <param name="filename">The file to open.</param>
        /// <returns>A result holding the bitmap or the reason it failed.</returns>
        public static Result<Bitmap> TryLoad(string filename)
        {
            try
            {
                Bitmap bitmap = Load(filename);
                return Result<Bitmap>.Pass(bitmap);
            }
            catch (FileNotFoundException e)
            {
                return Result<Bitmap>.Fail(filename + " not found: " + e.Message, 404);
            }
            catch (Exception e)
            {
                return Result<Bitmap>.Fail(e.Message, -1);
            }
        }

        //
        // HELPER CLASSES
        //////////////////

        /// <summary>
        /// A parser for palette based indexing that requires breaking down bytes.
        /// </summary>
        /// <param name="bitsPerPixel">The number of bits used to index into the palette.</param>
        private abstract class PaletteIndexParser(ColourDepth bitsPerPixel)
        {
            /// <summary>
            /// The current bit that the parser is on.
            /// </summary>
            protected int CurrentBit { get; private set; }

            /// <summary>
            /// How much the current bit is increased by each read.
            /// </summary>
            private readonly int _increment = (int)bitsPerPixel;

            /// <summary>
            /// Checks to see if the current bit is at the end of the 
            /// current byte, as in, the current byte has been fully parsed.
            /// </summary>
            /// <returns>True should the current byte be finished.</returns>
            internal bool IsByteFinished()
            {
                if (CurrentBit == 8)
                {
                    CurrentBit = 0;
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Parse the given byte.
            /// </summary>
            /// <param name="value">The byte to parse.</param>
            /// <returns>The parsed index from the byte.</returns>
            internal int Parse(byte value)
            {
                int returnValue = SubParse(value);
                CurrentBit += _increment;
                return returnValue;
            }

            /// <summary>
            /// Used to signify that the parser should 
            /// be reset to parse the next row.
            /// </summary>
            internal void NewRow()
            {
                CurrentBit = 0;
            }

            /// <summary>
            /// The specific implementation of the parsing of the byte.
            /// </summary>
            /// <param name="value">The byte to parse.</param>
            /// <returns>The index in the palette parsed out from the byte.</returns>
            protected abstract int SubParse(byte value);
        }

        /// <summary>
        /// Monochromatic (1-bit) bitmap parser. Goes through each bit of the byte individually.
        /// </summary>
        private sealed class MonochromaticParser : PaletteIndexParser
        {
            internal MonochromaticParser() : base(ColourDepth.Monochromatic) { }
            protected override int SubParse(byte value) => (value >> (7 - CurrentBit)) & 0x01;
        }

        /// <summary>
        /// 'CGA' (2-bit) bitmap parser. Reads two bits at a time.
        /// </summary>
        private sealed class CGAParser : PaletteIndexParser
        {
            internal CGAParser() : base(ColourDepth.CGA) { }
            protected override int SubParse(byte value) => (value >> (6 - CurrentBit)) & 0x03;
        }

        /// <summary>
        /// 'EGA' (4-bit) parser. Reads a nibble (4-bits) at a time.
        /// </summary>
        private sealed class EGAParser : PaletteIndexParser
        {
            internal EGAParser() : base(ColourDepth.EGA) { }
            protected override int SubParse(byte value) => (value >> (4 - CurrentBit)) & 0x0F;
        }
    }
}
