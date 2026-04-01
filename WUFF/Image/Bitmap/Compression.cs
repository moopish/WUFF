using WUFF.Bytes;
using WUFF.Err;

namespace WUFF.Image.Bitmap
{
    internal static class Compression
    {
        /// <summary>
        /// If the first byte on a read is this value (0) it signals 
        /// that an escape code is in the second byte.
        /// </summary>
        private const int EscapeSignal = 0x00;

        /// <summary>
        /// Values that come after a <see cref="EscapeSignal"/>. If the 
        /// observed value is not one of these, then it uses absolute 
        /// mode, where the indicies are listed out explicitly.
        /// </summary>
        private enum EscapeCode
        {
            /// <summary>
            /// Signals to end the line and start the next.
            /// </summary>
            EndLine = 0x00,
            
            /// <summary>
            /// Ends the decoding.
            /// </summary>
            EndDecoding = 0x01,

            /// <summary>
            /// Signals a jump. The next two bytes specify the jump.
            /// 00 02 xx yy where the 'xx' is the number of pixels 
            /// to jump horizontally and 'yy' the number of pixels 
            /// to jump vertically.
            /// </summary>
            Delta = 0x02
        }

        /// <summary>
        /// The possible compression types of a BMP file. Compression used can be
        /// found from the property <see cref="CompressionUsed"/>.
        /// </summary>
        internal enum Type
        {
            /// <summary>
            /// No compression used.
            /// </summary>
            None = 0,

            /// <summary>
            /// Run-length encoding for 8-bit, 256 colour BMPs.
            /// </summary>
            RLE8 = 1,

            /// <summary>
            /// Run-length encoding for 4-bit, 16 colour BMPs.
            /// </summary>
            RLE4 = 2,

            /// <summary>
            /// Encoding of colour channels based on bit masks.
            /// </summary>
            BitMasks = 3
        }

        /// <summary>
        /// Parse the pixel data based on bit masks.
        /// </summary>
        /// <param name="reader">The reader to read the bytes from.</param>
        /// <param name="info">The info header to get the masks and other bitmap details from.</param>
        /// <returns>The parsed pixels.</returns>
        internal static Colour[] ParseBitMask(LittleEndianReader reader, InfoHeader info)
        {
            if (info.Depth != ColourDepth.TrueColourWithAlpha && info.Depth != ColourDepth.HighColour)
                throw new FileParseException("Attempting to parse pixels with bit masks when unsupported for bitmap's colour depth.");

            Colour[] pixels = new Colour[info.Size];
            BitMaskParser parser = new(info.Masks);

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
        /// Decode RLE bitmaps. For 4-bit compression pass a <see cref="RLE4Parser"/> as the parser.
        /// For 8-bit compression pass a <see cref="RLE8Parser"/>.
        /// </summary>
        /// <param name="reader">The reader to get the data from.</param>
        /// <param name="info">The info header. Details the bitmap.</param>
        /// <param name="palette">The palette of the bitmap.</param>
        /// <param name="parser">The parser used to decode the bitmap.</param>
        /// <returns>The pixels of the decoded image.</returns>
        /// <exception cref="FileParseException">Thrown when issues arrise with trying to write outside of bitmap bounds or a negative height is provided.</exception>
        private static Colour[] RLEDecode(LittleEndianReader reader, InfoHeader info, Palette palette, RLEParser parser)
        {
            if (info.IsMirroredVertically) throw new FileParseException("RLE compression does not allow top-down (negative height).");

            Colour[] pixels = new Colour[info.Size];

            int x = 0;
            int y = info.Height - 1;

            bool running = true;

            do
            {
                byte count = reader.Byte();
                byte index = reader.Byte();

                if (count == EscapeSignal)
                {
                    // Escape code
                    switch ((EscapeCode)index)
                    {
                        case EscapeCode.EndLine:
                            --y;
                            x = 0;
                            break;

                        case EscapeCode.EndDecoding:
                            running = false;
                            break;

                        case EscapeCode.Delta:
                            x += reader.Byte();
                            y -= reader.Byte();
                            break;

                        default:
                            byte current = 0;

                            parser.StartAbsolute(index);

                            for (int i = 0; i < index; ++i)
                            {
                                if (x >= info.Width) throw new FileParseException("Decoding outside of width range.");
                                if (y < 0) throw new FileParseException("Decoding outside of height range.");

                                if (parser.IsByteFinished) current = reader.Byte();

                                pixels[y * info.Width + x++] = palette[parser.NextAbsoluteIndex(current)];
                            }
                            if (parser.IsAbsolutePadded) reader.Byte(); // Discard second byte of unit
                            break;
                    }
                }
                else
                {
                    parser.StartRun(index);

                    for (int i = 0; i < count; ++i)
                    {
                        if (x >= info.Width) throw new FileParseException("Decoding outside of width range.");
                        if (y < 0) throw new FileParseException("Decoding outside of height range.");

                        pixels[y * info.Width + x++] = palette[parser.NextRunIndex()];
                    }
                }
            } while (running);

            return pixels;
        }

        /// <summary>
        /// Decode RLE-4 pixel data.
        /// </summary>
        /// <param name="reader">The reader to get the data from.</param>
        /// <param name="info">The info header. Details the bitmap.</param>
        /// <param name="palette">The palette of the bitmap.</param>
        /// <returns>The pixels of the decoded image.</returns>
        /// <exception cref="FileParseException">Thrown when issues arrise with trying to write outside of bitmap bounds or a negative height is provided.</exception>
        internal static Colour[] RLE4Decode(LittleEndianReader reader, InfoHeader info, Palette palette) => RLEDecode(reader, info, palette, new RLE4Parser());

        /// <summary>
        /// Decode RLE-8 pixel data.
        /// </summary>
        /// <param name="reader">The reader to get the data from.</param>
        /// <param name="info">The info header. Details the bitmap.</param>
        /// <param name="palette">The palette of the bitmap.</param>
        /// <returns>The pixels of the decoded image.</returns>
        /// <exception cref="FileParseException">Thrown when issues arrise with trying to write outside of bitmap bounds or a negative height is provided.</exception>
        internal static Colour[] RLE8Decode(LittleEndianReader reader, InfoHeader info, Palette palette) => RLEDecode(reader, info, palette, new RLE8Parser());

        /// <summary>
        /// Parser to retrieve the ARGB values using bit masks.
        /// </summary>
        /// <param name="masks">The masks for the the colour and alpha channels.</param>
        private class BitMaskParser(BitMaskSet masks)
        {
            /// <summary>
            /// The bit masks for determining the colour and alpha channel values.
            /// </summary>
            private readonly BitMaskSet _masks = masks;

            /// <summary>
            /// Pull the colour and alpha channel data from the given 
            /// value using the bit masks.
            /// </summary>
            /// <param name="value">The value to get the colour from.</param>
            /// <returns>The colour from the value.</returns>
            internal Colour Parse(uint value)
            {
                uint alpha = 0;
                uint red = 0;
                uint green = 0;
                uint blue = 0;

                for (int bit = (int)_masks.Depth - 1; bit >= 0; --bit)
                {
                    uint bValue = (value >> bit) & 1;

                    switch (_masks.GetChannel(bit))
                    {
                        case ColourChannel.Alpha:
                            alpha = (alpha << 1) | bValue;
                            break;

                        case ColourChannel.Red:
                            red = (red << 1) | bValue;
                            break;

                        case ColourChannel.Green:
                            green = (green << 1) | bValue;
                            break;

                        case ColourChannel.Blue:
                            blue = (blue << 1) | bValue;
                            break;
                    }
                }

                alpha = ClampChannel(_masks.MaxAlpha, alpha);
                red = ClampChannel(_masks.MaxRed, red);
                green = ClampChannel(_masks.MaxGreen, green);
                blue = ClampChannel(_masks.MaxBlue, blue);

                if (_masks.MaxAlpha == 0) alpha = 255; // Assume no bitmask means no alpha.
                return Colour.FromARGB((byte)alpha, (byte)red, (byte)green, (byte)blue);
            }

            /// <summary>
            /// Clamp the given channel value to be between 0 and 255 (inclusive).
            /// </summary>
            /// <param name="max">The max value of the channel given the bit mask.</param>
            /// <param name="value">The value found using the bit mask.</param>
            /// <returns>The channel colour projected onto the 0 to 255 range.</returns>
            private static uint ClampChannel(uint max, uint value)
            {
                if (max == 0xFF || max == 0) return value;
                if (value == max) return 0xFF;
                return value * 0xFF / max;
            }
        }

        /// <summary>
        /// Represents the collection of bit masks to retrieve a colour.
        /// </summary>
        public class BitMaskSet
        {
            /// <summary>
            /// The default bit masks for a 16-bit bitmap. 5 bits 
            /// for each colour channel and a single unused bit.
            /// </summary>
            public readonly static BitMaskSet Default16Bit = new(0, 0b0111_1100_0000_0000, 0b0011_1110_0000, 0b0001_1111, ColourDepth.HighColour);
            
            /// <summary>
            /// The default bit masks for a 32-bit bitmap. 8 bits 
            /// for each colour channel and 8 unused bits.
            /// </summary>
            public readonly static BitMaskSet Default32Bit = new(0, 0x00_FF_00_00, 0x00_00_FF_00, 0x00_00_00_FF, ColourDepth.TrueColourWithAlpha);


            /// <summary>
            /// The colour depth of the bitmap image to parse.
            /// </summary>
            public readonly ColourDepth Depth;
            

            /// <summary>
            /// The alpha channel mask.
            /// </summary>
            public readonly uint Alpha;

            /// <summary>
            /// The red channel mask.
            /// </summary>
            public readonly uint Red;

            /// <summary>
            /// The green channel mask.
            /// </summary>
            public readonly uint Green;

            /// <summary>
            /// The blue channel mask.
            /// </summary>
            public readonly uint Blue;


            /// <summary>
            /// The max value possible for the alpha channel.
            /// </summary>
            public readonly uint MaxAlpha;

            /// <summary>
            /// The max value possible for the red channel.
            /// </summary>
            public readonly uint MaxRed;

            /// <summary>
            /// The max value possible for the green channel.
            /// </summary>
            public readonly uint MaxGreen; 

            /// <summary>
            /// The max value possible for the blue channel.
            /// </summary>
            public readonly uint MaxBlue;

            /// <summary>
            /// Initialize a <see cref="BitMaskSet"/>.
            /// </summary>
            /// <param name="alpha">The alpha mask.</param>
            /// <param name="red">The red mask.</param>
            /// <param name="green">The green mask.</param>
            /// <param name="blue">The blue mask.</param>
            /// <param name="depth">The colour depth.</param>
            /// <exception cref="FileParseException">Thrown should the bit masks overlap.</exception>
            public BitMaskSet(uint alpha, uint red, uint green, uint blue, ColourDepth depth)
            {
                Depth = depth;
                Alpha = alpha;
                Red = red;
                Green = green;
                Blue = blue;

                MaxAlpha = 0;
                MaxRed = 0;
                MaxGreen = 0;
                MaxBlue = 0;

                for (int i = 0; i < (int)depth; ++i)
                {
                    int count = 0;

                    if (((Red >> i) & 0x01) == 1) {
                        MaxRed = (MaxRed << 1) + 1;
                        ++count;
                    }
                    
                    if (((Green >> i) & 0x01) == 1)
                    {
                        MaxGreen = (MaxGreen << 1) + 1;
                        ++count; 
                    }

                    if (((Blue >> i) & 0x01) == 1)
                    {
                        MaxBlue = (MaxBlue << 1) + 1;
                        ++count;
                    }

                    if (((Alpha >> i) & 0x01) == 1)
                    {
                        MaxAlpha = (MaxAlpha << 1) + 1;
                        ++count;
                    }

                    if (count > 1) throw new FileParseException("Bit masks overlap");
                }
            }

            /// <summary>
            /// Determine which channel a bit belongs to based on the bit masks.
            /// </summary>
            /// <param name="bit">The bit to check.</param>
            /// <returns>The channel that claims the given bit.</returns>
            public ColourChannel GetChannel(int bit)
            {
                if (((Alpha >> bit) & 1) == 1) return ColourChannel.Alpha;
                if (((Red >> bit) & 1) == 1) return ColourChannel.Red;
                if (((Green >> bit) & 1) == 1) return ColourChannel.Green;
                if (((Blue >> bit) & 1) == 1) return ColourChannel.Blue;
                return ColourChannel.None;
            }
        }

        /// <summary>
        /// Run-length encoding parser.
        /// </summary>
        private abstract class RLEParser
        {
            /// <summary>
            /// True if there is a padded byte that needs to be discarded after the absolute escaped 
            /// </summary>
            internal abstract bool IsAbsolutePadded { get; }

            /// <summary>
            /// True if the current byte finished. Next byte should be read.
            /// </summary>
            internal abstract bool IsByteFinished { get; }

            /// <summary>
            /// Get the next absolute index from the RLE encoding.
            /// </summary>
            /// <param name="currentByte">The current byte to parse.</param>
            /// <returns>The next index.</returns>
            internal abstract int NextAbsoluteIndex(byte currentByte);

            /// <summary>
            /// Get the next index for a run.
            /// </summary>
            /// <returns>The next index.</returns>
            internal abstract int NextRunIndex();

            /// <summary>
            /// Intializes for parsing an absolute stream of indicies (i.e. the indicies are not repeated and not compressed).
            /// </summary>
            /// <param name="count">The number of indicies to parse.</param>
            internal abstract void StartAbsolute(byte count);

            /// <summary>
            /// Intializes for parsing a run, where the same index (or indices) are repeated a certain number of times.
            /// </summary>
            /// <param name="index">The index (or indicies) that will be used for the run.</param>
            internal abstract void StartRun(byte index);
        }

        /// <summary>
        /// Parser for RLE 4.
        /// <list type="bullet">
        /// <item>For a standard run, the structure is nn xy, where nn is the number of pixels (1-255),
        /// x (0-15) is the first index and y (0-15) is the second index. x and y will be alternated 
        /// for the nn pixels. E.g. 05 4F will give the stream of pixels: 4 F 4 F 4.</item>
        /// <item>For an absolute run, the structure is 00 nn a0 a1 .. an, where nn is the number of 
        /// pixels (3-255), and each ai for 0 to nn is byte holding an upper and lower nibble for the 
        /// run. The upper nibble is used first, then the lower. These runs are padded to the nearest
        /// 2-bytes. E.g. 00 05 4F 3E 10 00 gives the stream of pxiels: 4 F 3 E 1.</item>
        /// </list>
        /// </summary>
        private sealed class RLE4Parser : RLEParser
        {
            /// <summary>
            /// When true the second nibble of the byte has been parsed and the next 
            /// byte needs to be read for absolute or the upper nibble index should 
            /// be returned when doing a run. False when the lower nibble is needed
            /// for a run and in absolute not to read the next byte.
            /// </summary>
            private bool _flip;

            /// <summary>
            /// The upper nibble in a run (first, thrid, fifth... value to give)
            /// or the count of indicies that need to be read in an absolute state.
            /// Absolute runs are padded to the nearest 2-bytes.
            /// </summary>
            private byte _upper_or_value;

            /// <summary>
            /// The lower nibble in a run. Not used in an absolute state.
            /// </summary>
            private byte _lower;

            internal override bool IsByteFinished => _flip;

            internal override bool IsAbsolutePadded => (_upper_or_value & 3) == 1 || (_upper_or_value & 3) == 2;

            internal override int NextAbsoluteIndex(byte currentByte)
            {
                int index = _flip ? (currentByte >> 4) & 0x0F : currentByte & 0x0F;
                _flip = !_flip;
                return index;
            }

            internal override int NextRunIndex()
            {
                int index = _flip ? _upper_or_value : _lower;
                _flip = !_flip;
                return index;
            }

            internal override void StartAbsolute(byte count)
            {
                _flip = true;
                _upper_or_value = count;
            }

            internal override void StartRun(byte index)
            {
                _flip = true;
                _upper_or_value = (byte)((index >> 4) & 0x0F);
                _lower = (byte)(index & 0x0F);
            }
        }

        /// <summary>
        /// Parser for RLE 8.
        /// <list type="bullet">
        /// <item>For a standard run, the structure is nn xx, where nn is the number of pixels (1-255),
        /// xx (0-255) is the index. E.g. 05 4F will give the stream of pixels: 4F 4F 4F 4F 4F.</item>
        /// <item>For an absolute run, the structure is 00 nn a0 a1 .. an, where nn is the number of 
        /// pixels (3-255), and each ai for 0 to nn is an index for the run. These runs are padded to 
        /// the nearest 2-bytes. E.g. 00 03 4F 3E 10 00 gives the stream of pxiels: 4F 3E 10.</item>
        /// </list>
        /// </summary>
        private sealed class RLE8Parser : RLEParser
        {
            /// <summary>
            /// The index for a run, the indicies count for absolute.
            /// 
            /// </summary>
            private byte _value;

            internal override bool IsByteFinished => true;

            internal override bool IsAbsolutePadded => (_value & 1) == 1;

            internal override int NextAbsoluteIndex(byte currentByte) => currentByte;

            internal override int NextRunIndex() => _value;

            internal override void StartAbsolute(byte count)
            {
                _value = count;
            }

            internal override void StartRun(byte index)
            {
                _value = index;
            }
        }
    }
}
