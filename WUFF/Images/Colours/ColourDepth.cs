namespace WUFF.Images.Colours
{
    /// <summary>
    /// Possible colour depths. The number of bits per pixel (bpp). 
    /// </summary>
    public enum ColourDepth
    {
        /// <summary>
        /// One bit, two colours. For example, black and white.
        /// </summary>
        Monochromatic = 1,

        /// <summary>
        /// Two bits, four colours. Used CGA (Color Graphics Adapter) 
        /// as the reference as that provided 4-bit colour.
        /// </summary>
        CGA = 2,

        /// <summary>
        /// Four bits, sixteen colours. Used EGA (Enhanced Graphics Adapter) 
        /// as the reference as that provided 4-bit colour.
        /// </summary>
        EGA = 4,

        /// <summary>
        /// Eight bits, 256 colours. Used VGA (Video Graphics Array) 
        /// as the reference as that provided 8-bit colour.
        /// </summary>
        VGA = 8,

        /// <summary>
        /// Sixteen bits.
        /// </summary>
        HighColour = 16,

        /// <summary>
        /// 24-bits, 8 bits per colour.
        /// </summary>
        TrueColour = 24,

        /// <summary>
        /// 32-bits, 8 bits per colour and an 8 bit alpha.
        /// </summary>
        TrueColourWithAlpha = 32
    }
}
