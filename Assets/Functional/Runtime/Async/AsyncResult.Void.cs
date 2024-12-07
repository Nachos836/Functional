#nullable enable

using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Threading;

using static System.Runtime.CompilerServices.MethodImplOptions;

namespace Functional.Async
{
    using Core.Outcome;

    using static AsyncResultVoidIncomeSource;

    public readonly struct AsyncResult
    {
        private readonly AsyncResultVoidIncomeSource _income;
        private readonly Exception _exception;

        [MethodImpl(AggressiveInlining)]
        private AsyncResult(Success _)
        {
            _income = Succeed;
            _exception = default!;
        }

        [MethodImpl(AggressiveInlining)]
        private AsyncResult(CancellationToken _)
        {
            _income = Canceled;
            _exception = default!;
        }

        [MethodImpl(AggressiveInlining)]
        private AsyncResult(Exception exception)
        {
            _income = Exception;
            _exception = exception;
        }

        [Pure] public static AsyncResult Success { get; } = new (Expected.Success);
        [Pure] public static AsyncResult Cancel { get; } = new (CancellationToken.None);
        [Pure] public static AsyncResult Error { get; } = new (Unexpected.Error);
        [Pure] public static AsyncResult Impossible { get; } = new (Unexpected.Impossible);

        [Pure] public bool IsSuccessful => _income == Succeed;
        [Pure] public bool IsCancellation => _income == Canceled;
        [Pure] public bool IsFailure => _income != Succeed && _income != Canceled;

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static implicit operator AsyncResult (CancellationToken cancellation) => new (cancellation);
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static implicit operator AsyncResult (Exception error) => new (error);

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static AsyncResult FromCancellation(CancellationToken cancellation) => cancellation;
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static AsyncResult FromException(Exception exception) => exception;

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public AsyncRichResult AsRichResult()
        {
            return _income switch
            {
                Succeed => AsyncRichResult.Success,
                Canceled => AsyncRichResult.Cancel,
                Exception => AsyncRichResult.Error,
                _ => AsyncRichResult.Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public AsyncResult<TAnother> Attach<TAnother>(TAnother another)
        {
            return _income switch
            {
                Exception => _exception,
                Canceled => AsyncResult<TAnother>.Cancel,
                Succeed => another,
                _ => AsyncResult<TAnother>.Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public AsyncResult<TFirst, TSecond> Attach<TFirst, TSecond>(TFirst first, TSecond second)
        {
            return _income switch
            {
                Exception => _exception,
                Canceled => AsyncResult<TFirst, TSecond>.Cancel,
                Succeed => (first, second),
                _ => AsyncResult<TFirst, TSecond>.Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public AsyncResult<TFirst, TSecond, TThird> Attach<TFirst, TSecond, TThird>(TFirst first, TSecond second, TThird third)
        {
            return _income switch
            {
                Exception => _exception,
                Canceled => AsyncResult<TFirst, TSecond, TThird>.Cancel,
                Succeed => (first, second, third),
                _ => AsyncResult<TFirst, TSecond, TThird>.Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public AsyncResult Combine(AsyncResult another)
        {
            return (_income, another._income) switch
            {
                (Exception, Exception) => new AggregateException(_exception, another._exception),
                (Exception, _) => _exception,
                (_, Exception) => another._exception,
                (Canceled, Canceled) => Cancel,
                (Canceled, Succeed) => Cancel,
                (Succeed, Canceled) => Cancel,
                (Succeed, Succeed) => this,
                _ => Impossible
            };
        }

        [MethodImpl(AggressiveInlining)]
        public void Run(Action action)
        {
            if (_income == Succeed)
            {
                action.Invoke();
            }
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public AsyncResult Run(Func<AsyncResult> action)
        {
            return _income switch
            {
                Exception => _exception,
                Canceled => Cancel,
                Succeed => action.Invoke(),
                _ => Impossible
            };
        }

        [MethodImpl(AggressiveInlining)]
        public void Match
        (
            Action<CancellationToken> success,
            Action cancellation,
            Action<Exception> error,
            CancellationToken token = default
        ) {
            switch (_income)
            {
                case Exception:
                    error.Invoke(_exception);
                    return;
                case Canceled:
                    cancellation.Invoke();
                    return;
                case Succeed:
                    success.Invoke(token);
                    return;
                default:
                    error.Invoke(Unexpected.Impossible);
                    return;
            }
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public TMatch Match<TMatch>
        (
            Func<CancellationToken, TMatch> success,
            Func<TMatch> cancellation,
            Func<Exception, TMatch> error,
            CancellationToken token = default
        ) {
            return _income switch
            {
                Exception => error.Invoke(_exception),
                Canceled => cancellation.Invoke(),
                Succeed => success.Invoke(token),
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
                Canceled => nameof(Canceled),
                Succeed => nameof(Success),
                _ => new Exception().Message
            };
        }
    }

    internal enum AsyncResultVoidIncomeSource
    {
        Succeed = 1,
        Canceled = 2,
        Exception = 3
    }
}
