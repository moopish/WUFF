namespace WUFF.Image
{
    /// <summary>
    /// Represents an image.
    /// </summary>
    /// <param name="width">The width of the image.</param>
    /// <param name="height">The height of the image.</param>
    public abstract class Image(int width, int height)
    {
        /// <summary>
        /// The width of the bitmap image.
        /// </summary>
        public int Width { get; protected set; } = width;

        /// <summary>
        /// The height of the bitmap image.
        /// </summary>
        public int Height { get; protected set; } = height;

        /// <summary>
        /// Get the pixel at the specified position. Internal implimentation of a subclass.
        /// </summary>
        /// <param name="x">The horizontal position of the pixel.</param>
        /// <param name="y">The veritical position of the pixel.</param>
        /// <returns>The pixel at the specified position.</returns>
        protected abstract Colour SubGetPixel(int x, int y);

        /// <summary>
        /// Set the pixel at the given position. Internal implimentation of a subclass.
        /// </summary>
        /// <param name="x">The horizontal position of the pixel to set.</param>
        /// <param name="y">The vertical position of the pixel to set.</param>
        /// <param name="colour">The colour to set the pixel to.</param>
        protected abstract void SubSetPixel(int x, int y, Colour colour);

        /// <summary>
        /// Check if the provided coordinate values are within the area of the bitmap.
        /// </summary>
        /// <param name="x">The position of the pixel on the horizontal axis.</param>
        /// <param name="y">The position of the pixel on the vertical axis.</param>
        private void CheckCoordinates(int x, int y)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(x, nameof(x));
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(x, Width, nameof(x));
            ArgumentOutOfRangeException.ThrowIfNegative(y, nameof(y));
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(y, Height, nameof(y));
        }

        /// <summary>
        /// Get the pixel at the specified position.
        /// </summary>
        /// <param name="x">The horizontal position of the pixel.</param>
        /// <param name="y">The veritical position of the pixel.</param>
        /// <returns>The pixel at the specified position.</returns>
        public Colour GetPixel(int x, int y)
        {
            CheckCoordinates(x, y);
            return SubGetPixel(x, y) ?? Colour.Transparent;
        }

        /// <summary>
        /// Set the pixel at the given position.
        /// </summary>
        /// <param name="x">The horizontal position of the pixel to set.</param>
        /// <param name="y">The vertical position of the pixel to set.</param>
        /// <param name="colour">The colour to set the pixel to.</param>
        public void SetPixel(int x, int y, Colour colour)
        {
            CheckCoordinates(x, y);
            SubSetPixel(x, y, colour);
        }
    }
}
