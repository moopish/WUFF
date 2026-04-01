using System.Drawing;

namespace WUFF.Image
{
    public abstract class Colour
    {
        public readonly static Colour Black = FromRGB(0, 0, 0);
        public readonly static Colour Transparent = FromARGB(0, 0, 0, 0);
        public readonly static Colour White = FromRGB(255, 255, 255);

        public static Colour FromRGB(byte red, byte green, byte blue)
        {
            return new ARGB(Color.FromArgb(255, red, green, blue));
        }

        public static Colour FromARGB(byte alpha, byte red, byte green, byte blue)
        {
            return new ARGB(Color.FromArgb(alpha, red, green, blue));
        }

        public abstract Color ToColor();

        private class ARGB : Colour
        {
            private Color _colour;

            public ARGB(Color colour)
            {
                _colour = colour;
            }

            public override Color ToColor() => _colour;
        }
    }
}
