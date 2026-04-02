using WUFF.Bytes;
using WUFF.Err;
using WUFF.Image.Colours;

namespace WUFF.Image.Bitmaps
{
    /// <summary>
    /// Used for compression related to <see cref="Bitmap"/>.
    /// </summary>
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
