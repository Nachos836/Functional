#nullable enable

using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

using static System.Runtime.CompilerServices.MethodImplOptions;

namespace Functional
{
    using Outcome;

    using static RichResultVoidIncomeSource;

    public readonly struct RichResult
    {
        private readonly RichResultVoidIncomeSource _income;
        private readonly Exception _exception;
        private readonly Expected.Failure _failure;

        [MethodImpl(AggressiveInlining)]
        private RichResult(Success _)
        {
            _income = Succeed;
            _failure = default!;
            _exception = default!;
        }

        [MethodImpl(AggressiveInlining)]
        private RichResult(Expected.Failure failure)
        {
            _income = Failed;
            _failure = failure;
            _exception = default!;
        }

        [MethodImpl(AggressiveInlining)]
        private RichResult(Exception exception)
        {
            _income = Exception;
            _failure = default;
            _exception = exception;
        }

        [Pure] public static RichResult Success { get; } = new (Expected.Success);
        [Pure] public static RichResult Failure { get; } = new (Expected.Failed);
        [Pure] public static RichResult Error { get; } = new (Unexpected.Error);
        [Pure] public static RichResult Impossible { get; } = new (Unexpected.Impossible);

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static implicit operator RichResult (Expected.Failure expected) => new (expected);
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static implicit operator RichResult (Exception unexpected) => new (unexpected);

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static RichResult FromFailure(Expected.Failure failure) => failure;
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static RichResult FromException(Exception exception) => exception;

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public RichResult<TAnother> Attach<TAnother>(TAnother another)
        {
            return _income switch
            {
                Exception => _exception,
                Failed => _failure,
                Succeed => another,
                _ => RichResult<TAnother>.Impossible
            };
        }

        [MethodImpl(AggressiveInlining)]
        public void Match
        (
            Action success,
            Action<Expected.Failure> failure,
            Action<Exception> error
        ) {
            switch (_income)
            {
                case Exception:
                    error.Invoke(_exception);
                    return;
                case Failed:
                    failure.Invoke(_failure);
                    return;
                case Succeed:
                    success.Invoke();
                    return;
                default:
                    error.Invoke(Unexpected.Impossible);
                    return;
            }
        }

        [MethodImpl(AggressiveInlining)]
        public TMatch Match<TMatch>
        (
            Func<TMatch> success,
            Func<Expected.Failure, TMatch> failure,
            Func<Exception, TMatch> error
        ) {
            return _income switch
            {
                Exception => error.Invoke(_exception),
                Failed => failure.Invoke(_failure),
                Succeed => success.Invoke(),
                _ => error.Invoke(Unexpected.Impossible)
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public override string ToString()
        {
            return _income switch
            {
                Exception => _exception.Message,
                Failed => _failure.ToString(),
                Succeed => nameof(Success),
                _ => Unexpected.Impossible.Message
            };
        }
    }

    internal enum RichResultVoidIncomeSource
    {
        Succeed = 1,
        Failed = 2,
        Exception = 3
    }
}
