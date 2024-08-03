using System;
using System.Diagnostics.Contracts;

namespace Functional.Outcome
{
    public static class Expected
    {
        [Pure] public static Success Success => default;
        [Pure] public static None None => default;
        [Pure] public static Failure Failed { get; } = new (reason: "Execution Failed.");

        public readonly struct Failure
        {
            private readonly Lazy<Exception> _exception;
            public readonly string Message;

            public Failure(string reason)
            {
                Message = reason;
                _exception = new Lazy<Exception>(() => new Exception(reason));
            }

            [Pure]
            public Exception AsException() => _exception.Value;

            [Pure]
            public Failure Combine(Failure another) => new (Message + " and " + another.Message);

            [Pure]
            public override string ToString() => Message;
        }
    }
}
