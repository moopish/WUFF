namespace WUFF.Helpers
{
    internal static class Conditional
    {
        /// <summary>
        /// Check if all the values in <c>toCheck</c>
        /// are under or equal to <c>value</c>.
        /// </summary>
        /// <param name="value">The value that the values of toCheck should be equal to or less than.</param>
        /// <param name="toCheck">The values to check.</param>
        /// <returns>True if all the values stored in toCheck are under the given value, false otherwise.</returns>
        internal static bool AllUnderOrEqual(byte value, params byte[] toCheck) => toCheck.All((current) => current <= value);

        /// <summary>
        /// Check if all the values in <c>toCheck</c>
        /// are under or equal to <c>value</c>.
        /// </summary>
        /// <param name="value">The value that the values of toCheck should be equal to or less than.</param>
        /// <param name="toCheck">The values to check.</param>
        /// <returns>True if all the values stored in toCheck are under the given value, false otherwise.</returns>
        internal static bool AllUnderOrEqual(char value, params char[] toCheck) => toCheck.All((current) => current <= value);

        /// <summary>
        /// Check if all the values in <c>toCheck</c>
        /// are under or equal to <c>value</c>.
        /// </summary>
        /// <param name="value">The value that the values of toCheck should be equal to or less than.</param>
        /// <param name="toCheck">The values to check.</param>
        /// <returns>True if all the values stored in toCheck are under the given value, false otherwise.</returns>
        internal static bool AllUnderOrEqual(double value, params double[] toCheck) => toCheck.All((current) => current <= value);

        /// <summary>
        /// Check if all the values in <c>toCheck</c>
        /// are under or equal to <c>value</c>.
        /// </summary>
        /// <param name="value">The value that the values of toCheck should be equal to or less than.</param>
        /// <param name="toCheck">The values to check.</param>
        /// <returns>True if all the values stored in toCheck are under the given value, false otherwise.</returns>
        internal static bool AllUnderOrEqual(float value, params float[] toCheck) => toCheck.All((current) => current <= value);

        /// <summary>
        /// Check if all the values in <c>toCheck</c>
        /// are under or equal to <c>value</c>.
        /// </summary>
        /// <param name="value">The value that the values of toCheck should be equal to or less than.</param>
        /// <param name="toCheck">The values to check.</param>
        /// <returns>True if all the values stored in toCheck are under the given value, false otherwise.</returns>
        internal static bool AllUnderOrEqual(int value, params int[] toCheck) => toCheck.All((current) => current <= value);

        /// <summary>
        /// Check if all the values in <c>toCheck</c>
        /// are under or equal to <c>value</c>.
        /// </summary>
        /// <param name="value">The value that the values of toCheck should be equal to or less than.</param>
        /// <param name="toCheck">The values to check.</param>
        /// <returns>True if all the values stored in toCheck are under the given value, false otherwise.</returns>
        internal static bool AllUnderOrEqual(long value, params long[] toCheck) => toCheck.All((current) => current <= value);

        /// <summary>
        /// Check if all the values in <c>toCheck</c>
        /// are under or equal to <c>value</c>.
        /// </summary>
        /// <param name="value">The value that the values of toCheck should be equal to or less than.</param>
        /// <param name="toCheck">The values to check.</param>
        /// <returns>True if all the values stored in toCheck are under the given value, false otherwise.</returns>
        internal static bool AllUnderOrEqual(sbyte value, params sbyte[] toCheck) => toCheck.All((current) => current <= value);

        /// <summary>
        /// Check if all the values in <c>toCheck</c>
        /// are under or equal to <c>value</c>.
        /// </summary>
        /// <param name="value">The value that the values of toCheck should be equal to or less than.</param>
        /// <param name="toCheck">The values to check.</param>
        /// <returns>True if all the values stored in toCheck are under the given value, false otherwise.</returns>
        internal static bool AllUnderOrEqual(short value, params short[] toCheck) => toCheck.All((current) => current <= value);

        /// <summary>
        /// Check if all the values in <c>toCheck</c>
        /// are under or equal to <c>value</c>.
        /// </summary>
        /// <param name="value">The value that the values of toCheck should be equal to or less than.</param>
        /// <param name="toCheck">The values to check.</param>
        /// <returns>True if all the values stored in toCheck are under the given value, false otherwise.</returns>
        internal static bool AllUnderOrEqual(uint value, params uint[] toCheck) => toCheck.All((current) => current <= value);

        /// <summary>
        /// Check if all the values in <c>toCheck</c>
        /// are under or equal to <c>value</c>.
        /// </summary>
        /// <param name="value">The value that the values of toCheck should be equal to or less than.</param>
        /// <param name="toCheck">The values to check.</param>
        /// <returns>True if all the values stored in toCheck are under the given value, false otherwise.</returns>
        internal static bool AllUnderOrEqual(ulong value, params ulong[] toCheck) => toCheck.All((current) => current <= value);

        /// <summary>
        /// Check if all the values in <c>toCheck</c>
        /// are under or equal to <c>value</c>.
        /// </summary>
        /// <param name="value">The value that the values of toCheck should be equal to or less than.</param>
        /// <param name="toCheck">The values to check.</param>
        /// <returns>True if all the values stored in toCheck are under the given value, false otherwise.</returns>
        internal static bool AllUnderOrEqual(ushort value, params ushort[] toCheck) => toCheck.All((current) => current <= value);
    }
}
