namespace WUFF.Err
{
    /// <summary>
    /// Exception for issues related to file parsing.
    /// </summary>
    public sealed class FileParseException : Exception
    {
        /// <summary>
        /// Create an unspecified parse exception.
        /// </summary>
        public FileParseException() : base("Unspecified parse exception") { }

        /// <summary>
        /// Create a parse exception with a provied message to explain the issue.
        /// </summary>
        /// <param name="message">Message to provide context as to what caused the exception.</param>
        public FileParseException(string message) : base(message) { }

        /// <summary>
        /// Throw exception if the provided value is negative.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="message">The message to provide when the exception is thrown.</param>
        /// <exception cref="FileParseException">If the value is negative.</exception>
        public static void ThrowIfNegative(int value, string message)
        {
            if (value < 0) throw new FileParseException(message);
        }

        /// <summary>
        /// Throw exception if the provided offset is negative.
        /// </summary>
        /// <param name="offset">The offset to check.</param>
        /// <exception cref="FileParseException">If the offset is negative.</exception>
        public static void ThrowIfNegativeOffset(int offset) => ThrowIfNegative(offset, "Negative offset provided.");

        /// <summary>
        /// If the value is not the expected value, throw an execption.
        /// </summary>
        /// <typeparam name="T">The type of the values to compare.</typeparam>
        /// <param name="actual">The actual value at runtime.</param>
        /// <param name="expected">The expected value.</param>
        /// <param name="message">The message to give to detail the exception.</param>
        /// <exception cref="FileParseException">Thrown if actual and expected are not equal.</exception>
        public static void ThrowIfNotEqual<T>(T actual, T expected, string message)
        {
            if (actual == null && expected != null || actual != null && !actual.Equals(expected))
                throw new FileParseException(message);
        }
    }
}
