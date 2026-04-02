using System.Drawing;
using WUFF.Helpers;

namespace WUFF.Image
{
    /// <summary>
    /// Represents a colour.
    /// </summary>
    public abstract class Colour
    {
        /// <summary>
        /// Represents the colour black.
        /// </summary>
        public readonly static Colour Black = FromRGB(0, 0, 0);

        /// <summary>
        /// Represents a transparent colour.
        /// </summary>
        public readonly static Colour Transparent = FromARGB(0, 0, 0, 0);

        /// <summary>
        /// Represents the colour white.
        /// </summary>
        public readonly static Colour White = FromRGB(255, 255, 255);

        /// <summary>
        /// Gets a Colour object that represents the colour with the
        /// given alpha, red, green, and blue channel values.
        /// </summary>
        /// <param name="alpha">The value of the alpha channel.</param>
        /// <param name="red">The value of the red channel.</param>
        /// <param name="green">The value of the green channel.</param>
        /// <param name="blue">The value of the blue channel.</param>
        /// <returns>The colour that represents the given channels.</returns>
        public static Colour FromARGB(byte alpha, byte red, byte green, byte blue)
        {
            return new ARGB(Color.FromArgb(alpha, red, green, blue));
        }

        /// <summary>
        /// Gets a Colour object that represents the colour with the
        /// given alpha, red, green, and blue channel values. It is 
        /// </summary>
        /// <param name="alpha">The value of the alpha channel.</param>
        /// <param name="red">The value of the red channel.</param>
        /// <param name="green">The value of the green channel.</param>
        /// <param name="blue">The value of the blue channel.</param>
        /// <returns>The colour that represents the given channels.</returns>
        public static Colour FromARGB(uint alpha, uint red, uint green, uint blue, MaxSet maximums)
        {
            if (maximums.ChannelsAtMost8Bits) return new SmallMappable((byte)alpha, (byte)red, (byte)green, (byte)blue, maximums);
            else if (maximums.ChannelsAtMost16Bits) return new MediumMappable((ushort)alpha, (ushort)red, (ushort)green, (ushort)blue, maximums);
            else return new LargeMappable(alpha, red, green, blue, maximums);
        }

        /// <summary>
        /// Gets a Colour object that represents the colour with the
        /// given red, green, and blue channel values.
        /// </summary>
        /// <param name="red">The value of the red channel.</param>
        /// <param name="green">The value of the green channel.</param>
        /// <param name="blue">The value of the blue channel.</param>
        /// <returns>The colour that represents the given channels.</returns>
        public static Colour FromRGB(byte red, byte green, byte blue)
        {
            return new ARGB(Color.FromArgb(255, red, green, blue));
        }

        /// <summary>
        /// Convert this <see cref="Colour"/> to a <see cref="Color"/>. May cause
        /// some precision loss for colours that have more than 8-bits in a channel.
        /// </summary>
        /// <returns>A <see cref="Color"/> that represents the same (or similar) 
        /// colour that this <see cref="Colour"/> represents.</returns>
        public abstract Color ToColor();

        /// <summary>
        /// Represents a ARGB 32-bit (8-bit channels) colour.
        /// </summary>
        /// <param name="colour">The internal value of the colour.</param>
        private class ARGB(Color colour) : Colour
        {
            public override Color ToColor() => colour;
        }

        /// <summary>
        /// Used to represent the channels if the channels are at most 8-bits.
        /// </summary>
        /// <param name="alpha">The value of the alpha channel.</param>
        /// <param name="red">The value of the red channel.</param>
        /// <param name="green">The value of the green channel.</param>
        /// <param name="blue">The value of the blue channel.</param>
        /// <param name="max">The maximum values possible for each channel.</param>
        private class SmallMappable(byte alpha, byte red, byte green, byte blue, MaxSet max) : Colour
        {
            public override Color ToColor() => Color.FromArgb((int)max.MapAlpha(alpha), (int)max.MapRed(red), (int)max.MapGreen(green), (int)max.MapBlue(blue));
        }

        /// <summary>
        /// Used to represent the channels if the channels are at most 16-bits.
        /// </summary>
        /// <param name="alpha">The value of the alpha channel.</param>
        /// <param name="red">The value of the red channel.</param>
        /// <param name="green">The value of the green channel.</param>
        /// <param name="blue">The value of the blue channel.</param>
        /// <param name="max">The maximum values possible for each channel.</param>
        private class MediumMappable(ushort alpha, ushort red, ushort green, ushort blue, MaxSet max) : Colour
        {
            public override Color ToColor() => Color.FromArgb((int)max.MapAlpha(alpha), (int)max.MapRed(red), (int)max.MapGreen(green), (int)max.MapBlue(blue));
        }

        /// <summary>
        /// Used to represent the channels if the channels are at most 32-bits.
        /// </summary>
        /// <param name="alpha">The value of the alpha channel.</param>
        /// <param name="red">The value of the red channel.</param>
        /// <param name="green">The value of the green channel.</param>
        /// <param name="blue">The value of the blue channel.</param>
        /// <param name="max">The maximum values possible for each channel.</param>
        private class LargeMappable(uint alpha, uint red, uint green, uint blue, MaxSet max) : Colour
        {
            public override Color ToColor() => Color.FromArgb((int)max.MapAlpha(alpha), (int)max.MapRed(red), (int)max.MapGreen(green), (int)max.MapBlue(blue));
        }

        /// <summary>
        /// Represents maximum possible values for colour channels.
        /// </summary>
        /// <param name="alpha">The max for the alpha channel.</param>
        /// <param name="red">The max for the red channel.</param>
        /// <param name="green">The max for the green channel.</param>
        /// <param name="blue">The max for the blue channel.</param>
        public sealed class MaxSet(uint alpha, uint red, uint green, uint blue)
        {
            /// <summary>
            /// The maximum value for the alpha channel.
            /// </summary>
            public uint Alpha { get; } = alpha;

            /// <summary>
            /// The maximum value for the red channel.
            /// </summary>
            public uint Red { get; } = red;

            /// <summary>
            /// The maximum value for the green channel.
            /// </summary>
            public uint Green { get; } = green;

            /// <summary>
            /// The maximum value for the blue channel.
            /// </summary>
            public uint Blue { get; } = blue;

            /// <summary>
            /// True if all the channels at most 8-bits.
            /// </summary>
            public bool ChannelsAtMost8Bits => Conditional.AllUnderOrEqual(byte.MaxValue, Alpha, Red, Green, Blue);

            /// <summary>
            /// True if all the channels at most 16-bits.
            /// </summary>
            public bool ChannelsAtMost16Bits => Conditional.AllUnderOrEqual(ushort.MaxValue, Alpha, Red, Green, Blue);

            /// <summary>
            /// Represents maximum possible values for colour channels.
            /// </summary>
            /// <param name="red">The max for the red channel.</param>
            /// <param name="green">The max for the green channel.</param>
            /// <param name="blue">The max for the blue channel.</param>
            public MaxSet(uint red, uint green, uint blue) : this (0, red, green, blue) { }

            /// <summary>
            /// Represents maximum possible values for colour channels.
            /// </summary>
            /// <param name="alpha">The max for the alpha channel.</param>
            /// <param name="colour">The max for the colour channels.</param>
            public MaxSet(uint alpha, uint colour) : this(alpha, colour, colour, colour) { }

            /// <summary>
            /// Represents maximum possible values for colour channels.
            /// </summary>
            /// <param name="colour">The max for the colour channels.</param>
            public MaxSet(uint colour) : this(0, colour, colour, colour) { }
            
            /// <summary>
            /// Map the given channel value to be between 0 and 255 (inclusive).
            /// </summary>
            /// <param name="max">The max value of the channel given the bit mask.</param>
            /// <param name="value">The value found using the bit mask.</param>
            /// <returns>The channel colour projected onto the 0 to 255 range.</returns>
            public static uint MapChannel(uint max, uint value)
            {
                if (max == 0xFF || max == 0) return value;
                if (value == max) return 0xFF;
                return (uint)((ulong)value * 0xFF / max);
            }

            /// <summary>
            /// Maps the given red channel value to be between 0 to 255 (proportionally).
            /// </summary>
            /// <param name="red">The value to map to the range 0 to 255.</param>
            /// <returns>The mapped value.</returns>
            public uint MapRed(uint red) => MapChannel(Red, red);

            /// <summary>
            /// Maps the given green channel value to be between 0 to 255 (proportionally).
            /// </summary>
            /// <param name="green">The value to map to the range 0 to 255.</param>
            /// <returns>The mapped value.</returns>
            public uint MapGreen(uint green) => MapChannel(Green, green);

            /// <summary>
            /// Maps the given blue channel value to be between 0 to 255 (proportionally).
            /// </summary>
            /// <param name="blue">The value to map to the range 0 to 255.</param>
            /// <returns>The mapped value.</returns>
            public uint MapBlue(uint blue) => MapChannel(Blue, blue);

            /// <summary>
            /// Maps the given alpha channel value to be between 0 to 255 (proportionally).
            /// </summary>
            /// <param name="alpha">The value to map to the range 0 to 255.</param>
            /// <returns>The mapped value.</returns>
            public uint MapAlpha(uint alpha) => MapChannel(Alpha, alpha);
        }
    }
}
