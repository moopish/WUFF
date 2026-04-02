using WUFF.Err;

namespace WUFF.Image
{
    /// <summary>
    /// Represents the collection of bit masks to retrieve a colour.
    /// </summary>
    internal class ColourMaskSet
    {
        /// <summary>
        /// The default bit masks for a 16-bit bitmap. 5 bits 
        /// for each colour channel and a single unused bit.
        /// </summary>
        public readonly static ColourMaskSet Default16Bit = new(0, 0b0111_1100_0000_0000, 0b0011_1110_0000, 0b0001_1111, ColourDepth.HighColour);

        /// <summary>
        /// The default bit masks for a 32-bit bitmap. 8 bits 
        /// for each colour channel and 8 unused bits.
        /// </summary>
        public readonly static ColourMaskSet Default32Bit = new(0, 0x00_FF_00_00, 0x00_00_FF_00, 0x00_00_00_FF, ColourDepth.TrueColourWithAlpha);


        /// <summary>
        /// The colour depth of the bitmap image to parse.
        /// </summary>
        public readonly ColourDepth Depth;


        /// <summary>
        /// The alpha channel mask.
        /// </summary>
        public readonly uint AlphaMask;

        /// <summary>
        /// The red channel mask.
        /// </summary>
        public readonly uint RedMask;

        /// <summary>
        /// The green channel mask.
        /// </summary>
        public readonly uint GreenMask;

        /// <summary>
        /// The blue channel mask.
        /// </summary>
        public readonly uint BlueMask;

        /// <summary>
        /// The maximum possible values for each channel.
        /// </summary>
        public readonly Colour.MaxSet MaxSet;

        /// <summary>
        /// Initialize a <see cref="BitMaskSet"/>.
        /// </summary>
        /// <param name="alpha">The alpha mask.</param>
        /// <param name="red">The red mask.</param>
        /// <param name="green">The green mask.</param>
        /// <param name="blue">The blue mask.</param>
        /// <param name="depth">The colour depth.</param>
        /// <exception cref="FileParseException">Thrown should the bit masks overlap.</exception>
        public ColourMaskSet(uint alpha, uint red, uint green, uint blue, ColourDepth depth)
        {
            Depth = depth;
            AlphaMask = alpha;
            RedMask = red;
            GreenMask = green;
            BlueMask = blue;

            uint maxAlpha = 0;
            uint maxRed = 0;
            uint maxGreen = 0;
            uint maxBlue = 0;

            for (int i = 0; i < (int)depth; ++i)
            {
                int count = 0;

                if (((RedMask >> i) & 0x01) == 1)
                {
                    maxRed = (maxRed << 1) + 1;
                    ++count;
                }

                if (((GreenMask >> i) & 0x01) == 1)
                {
                    maxGreen = (maxGreen << 1) + 1;
                    ++count;
                }

                if (((BlueMask >> i) & 0x01) == 1)
                {
                    maxBlue = (maxBlue << 1) + 1;
                    ++count;
                }

                if (((AlphaMask >> i) & 0x01) == 1)
                {
                    maxAlpha = (maxAlpha << 1) + 1;
                    ++count;
                }

                if (count > 1) throw new FileParseException("Bit masks overlap");
            }

            MaxSet = new Colour.MaxSet(maxAlpha, maxRed, maxGreen, maxBlue);
        }

        /// <summary>
        /// Determine which channel a bit belongs to based on the bit masks.
        /// </summary>
        /// <param name="bit">The bit to check.</param>
        /// <returns>The channel that claims the given bit.</returns>
        public ColourChannel GetChannel(int bit)
        {
            if (((AlphaMask >> bit) & 1) == 1) return ColourChannel.Alpha;
            if (((RedMask >> bit) & 1) == 1) return ColourChannel.Red;
            if (((GreenMask >> bit) & 1) == 1) return ColourChannel.Green;
            if (((BlueMask >> bit) & 1) == 1) return ColourChannel.Blue;
            return ColourChannel.None;
        }

        /// <summary>
        /// Pull the colour and alpha channel data from the given 
        /// value using the bit masks.
        /// </summary>
        /// <param name="value">The value to get the colour from.</param>
        /// <returns>The colour from the value.</returns>
        public Colour Parse(uint value)
        {
            uint alpha = 0;
            uint red = 0;
            uint green = 0;
            uint blue = 0;

            for (int bit = (int)Depth - 1; bit >= 0; --bit)
            {
                uint bValue = (value >> bit) & 1;

                switch (GetChannel(bit))
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

            if (MaxSet.Alpha == 0) alpha = 255; // Assume no bitmask means no alpha.
            return Colour.FromARGB(alpha, red, green, blue, MaxSet);
        }
    }
}
