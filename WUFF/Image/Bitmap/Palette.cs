using System.Drawing;
using WUFF.Bytes;

namespace WUFF.Image.Bitmap
{
    /// <summary>
    /// Represents a palette that a <see cref="Bitmap"/> uses to match
    /// indices to their appropriate colours.
    /// </summary>
    internal class Palette
    {
        /// <summary>
        /// An empty palette. Contains no colours.
        /// </summary>
        public static readonly Palette EmptyPalette = new([]);

        /// <summary>
        /// The colours stored in this palette.
        /// </summary>
        private readonly Color[] _colours;

        /// <summary>
        /// The number of colours in the palette.
        /// </summary>
        public int ColourCount => _colours.Length;

        /// <summary>
        /// Creates a palette given the array of colours.
        /// </summary>
        /// <param name="colours">The colours that the palette will contain.</param>
        private Palette(Color[] colours)
        {
            _colours = colours;
        }

        /// <summary>
        /// Gets the colour at the specified index.
        /// </summary>
        /// <param name="index">The index to get the colour from.</param>
        /// <returns>The colour at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Throws if the provided index is negative or larger than the max index.</exception>
        public Color this[int index]
        {
            get {
                ArgumentOutOfRangeException.ThrowIfNegative(index, "Palette index must be positive.");
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _colours.Length, "Palette index must be less than the number of colours in the palette.");
                return _colours[index]; 
            }
        }

        /// <summary>
        /// Checks if a given bitmap colour table will contain the alpha value.
        /// </summary>
        /// <param name="type">The header type that informs the version of the bitmap.</param>
        /// <returns>True if the bitmap has an alpha value in the colour table.</returns>
        public static bool HasAlpha(InfoHeader.HeaderType type)
        {
            return (int)type >= (int)InfoHeader.HeaderType.WindowsHeaderV1;
        }

        /// <summary>
        /// Parse the palette from the colour table that is found in the bitmap file.
        /// </summary>
        /// <param name="bytes">The bytes to parse the table from.</param>
        /// <param name="info">The details of the bitmap.</param>
        /// <param name="offset">The starting index in bytes of the colour table.</param>
        /// <returns>A palette containing the colours of the colour table.</returns>
        public static Palette Parse(Span<byte> bytes, InfoHeader info, uint offset = 0)
        {
            uint colourCount = info.ColoursInPalette;

            // No palette used.
            if (colourCount == 0) return EmptyPalette;

            Color[] colours = new Color[colourCount];

            LittleEndianReader reader = new(bytes, offset);

            for (int colour = 0; colour < colourCount; ++colour)
            {
                byte blue = reader.Byte();
                byte green = reader.Byte();
                byte red = reader.Byte();

                if (HasAlpha(info.Type))
                {
                    reader.Byte(); // Ignore alpha/reserved byte for getting the colour.
                }

                colours[colour] = Color.FromArgb(red, green, blue);
            }

            return new(colours);
        }
    }
}
