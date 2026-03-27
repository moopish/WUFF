namespace WUFF.Err
{
    /// <summary>
    /// A result is returned from a function to either provide 
    /// a success (give a value) or a failure (give a reason).
    /// </summary>
    /// <typeparam name="T">Type of the result.</typeparam>
    public abstract class Result<T>
    {
        /// <summary>
        /// Whether the result represents a failure or not.
        /// </summary>
        public abstract bool Failed { get; }

        /// <summary>
        /// Whether the result represents a success or not.
        /// </summary>
        public bool Passed => !Failed;

        /// <summary>
        /// Get the error code associated with the failure.
        /// Should be 0 if there is no error, but use 
        /// <see cref="Failed"/> or <see cref="Passed"/> to
        /// determine if the result is successful.
        /// </summary>
        /// <returns></returns>
        public abstract int GetErrorCode();

        /// <summary>
        /// Gets the result's value if successful. Throws an exception if not successful.
        /// </summary>
        /// <returns>The result value if successful.</returns>
        /// <exception cref="ResultException">Thrown if the result is a failure.</exception>
        public abstract T GetResult();

        /// <summary>
        /// The reason of the failure. Empty string if successful.
        /// </summary>
        /// <returns>The reason of the failure.</returns>
        public abstract string GetReason();

        /// <summary>
        /// A successful result. Contains the result value.
        /// </summary>
        /// <param name="result">The result value.</param>
        private sealed class SuccessfulResult(T result) : Result<T>
        {
            private readonly T _result = result;
            public override bool Failed => false;
            public override int GetErrorCode() => 0;
            public override T GetResult() => _result;
            public override string GetReason() => "";
        }

        /// <summary>
        /// Represents a failed result. Contains a reason for the failure.
        /// </summary>
        /// <param name="reason">The reason for the failure.</param>
        /// <param name="errorCode">Error code for the fail state.</param>
        private sealed class FailedResult(string reason, int errorCode) : Result<T>
        {
            private readonly int _errorCode = errorCode;
            private readonly string _reason = reason;
            public override bool Failed => true;
            public override int GetErrorCode() => _errorCode;
            public override T GetResult() => ResultException.NoResult(_reason);
            public override string GetReason() => _reason;
        }

        /// <summary>
        /// Represents errors related to <see cref="Result{T}"/>.
        /// </summary>
        public sealed class ResultException : Exception
        {
            private ResultException(string message) : base(message) { }

            /// <summary>
            /// Throws an exception given the provided reason.
            /// </summary>
            /// <param name="reason">The reason for the failure.</param>
            /// <returns>Nothing.</returns>
            /// <exception cref="ResultException">Will always be thrown.</exception>
            internal static T NoResult(string reason) => throw new ResultException("Failed result. Cannot use GetResult. Reason: " + reason);
        }

        /// <summary>
        /// Returns a failed result with the given message.
        /// </summary>
        /// <param name="message">The message describing the failure.</param>
        /// <param name="errCode">The error code of the failure.</param>
        /// <returns>A failed result.</returns>
        public static Result<T> Fail(string message, int errCode) => new FailedResult(message, errCode);
        
        /// <summary>
        /// Returns a successful result with the result value.
        /// </summary>
        /// <param name="result">The result value.</param>
        /// <returns>A successful result.</returns>
        public static Result<T> Pass(T result) => new SuccessfulResult(result);
    }
}
