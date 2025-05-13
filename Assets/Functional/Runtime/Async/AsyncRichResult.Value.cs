using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;

using static System.Runtime.CompilerServices.MethodImplOptions;

namespace Functional.Async
{
    using Core.Outcome;

    using static AsyncRichResultValueIncomeSource;

    public readonly struct AsyncRichResult<TValue>
    {
        private readonly AsyncRichResultValueIncomeSource _income;
        private readonly TValue _value;
        private readonly Expected.Failure _failure;
        private readonly Exception _exception;

        [MethodImpl(AggressiveInlining)]
        private AsyncRichResult(TValue value)
        {
            _income = Value;
            _value = value;
            _failure = default!;
            _exception = default!;
        }

        [MethodImpl(AggressiveInlining)]
        private AsyncRichResult(CancellationToken _)
        {
            _income = Canceled;
            _value = default!;
            _failure = default!;
            _exception = default!;
        }

        [MethodImpl(AggressiveInlining)]
        private AsyncRichResult(Expected.Failure failure)
        {
            _income = Failed;
            _value = default!;
            _failure = failure;
            _exception = default!;
        }

        [MethodImpl(AggressiveInlining)]
        private AsyncRichResult(Exception exception)
        {
            _income = Exception;
            _value = default!;
            _failure = default!;
            _exception = exception;
        }

        [Pure] public static AsyncRichResult<TValue> Cancel { get; } = new (CancellationToken.None);
        [Pure] public static AsyncRichResult<TValue> Failure { get; } = new (Expected.Failed);
        [Pure] public static AsyncRichResult<TValue> Error { get; } = new (Unexpected.Error);
        [Pure] public static AsyncRichResult<TValue> Impossible { get; } = new (Unexpected.Impossible);

        [Pure] public bool HasValue => _income == Value;
        [Pure] public bool IsCancellation => _income == Canceled;
        [Pure] public bool IsFailure => _income == Failed;
        [Pure] public bool IsError => (_income & (Value | Canceled | Failed)) != 0;

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static implicit operator AsyncRichResult<TValue> (TValue value) => new (value);
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static implicit operator AsyncRichResult<TValue> (CancellationToken cancellation) => new (cancellation);
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static implicit operator AsyncRichResult<TValue> (Expected.Failure failure) => new (failure);
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static implicit operator AsyncRichResult<TValue> (Exception error) => new (error);

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static AsyncRichResult<TValue> FromResult(TValue value) => value;
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static AsyncRichResult<TValue> FromCancellation(CancellationToken cancellation) => cancellation;
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static AsyncRichResult<TValue> FromFailure(Expected.Failure failure) => failure;
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static AsyncRichResult<TValue> FromException(Exception exception) => exception;

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public AsyncResult<TValue> AsAsyncResult()
        {
            return _income switch
            {
                Exception => _exception,
                Failed => _failure.AsException(),
                Canceled => AsyncResult<TValue>.Cancel,
                Value => AsyncResult<TValue>.FromResult(_value),
                _ => AsyncResult<TValue>.Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public AsyncRichResult AsAsyncRichResult()
        {
            return _income switch
            {
                Exception => _exception,
                Failed => _failure,
                Canceled => AsyncRichResult.Cancel,
                Value => AsyncRichResult.Success,
                _ => AsyncRichResult.Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public AsyncRichResult<TValue> Run(Action<TValue> action)
        {
            if (HasValue)
            {
                action.Invoke(_value);
            }

            return this;
        }

        [MethodImpl(AggressiveInlining)]
        public void Match
        (
            Action<TValue, CancellationToken> success,
            Action cancellation,
            Action<Expected.Failure> failure,
            Action<Exception> error,
            CancellationToken token = default
        ) {
            switch (_income)
            {
                case Exception:
                    error.Invoke(_exception);
                    return;
                case Failed:
                    failure.Invoke(_failure);
                    return;
                case Canceled:
                    cancellation.Invoke();
                    return;
                case Value:
                    success.Invoke(_value, token);
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
            Func<TValue, CancellationToken, TMatch> success,
            Func<TMatch> cancellation,
            Func< Expected.Failure, TMatch> failure,
            Func<Exception, TMatch> error,
            CancellationToken token = default
        ) {
            return _income switch
            {
                Exception => error.Invoke(_exception),
                Failed => failure.Invoke(_failure),
                Canceled => cancellation.Invoke(),
                Value => success.Invoke(_value, token),
                _ => error.Invoke(Unexpected.Impossible)
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public UniTask MatchAsync
        (
            Func<TValue, CancellationToken, UniTask> success,
            Func<UniTask> cancellation,
            Func<Expected.Failure, UniTask> failure,
            Func<Exception, UniTask> error,
            CancellationToken token = default
        ) {
            return _income switch
            {
                Exception => error.Invoke(_exception),
                Failed => failure.Invoke(_failure),
                Canceled => cancellation.Invoke(),
                Value => success.Invoke(_value, token),
                _ => error.Invoke(Unexpected.Impossible)
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public UniTask MatchAsync
        (
            Func<TValue, CancellationToken, UniTask> success,
            Func<CancellationToken, UniTask> cancellation,
            Func<Expected.Failure, CancellationToken, UniTask> failure,
            Func<Exception, CancellationToken, UniTask> error,
            CancellationToken token = default
        ) {
            return _income switch
            {
                Exception => error.Invoke(_exception, token),
                Failed => failure.Invoke(_failure, token),
                Canceled => cancellation.Invoke(token),
                Value => success.Invoke(_value, token),
                _ => error.Invoke(Unexpected.Impossible, token)
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public UniTask<TMatch> MatchAsync<TMatch>
        (
            Func<TValue, CancellationToken, UniTask<TMatch>> success,
            Func<UniTask<TMatch>> cancellation,
            Func<Expected.Failure, UniTask<TMatch>> failure,
            Func<Exception, UniTask<TMatch>> error,
            CancellationToken token = default
        ) {
            return _income switch
            {
                Exception => error.Invoke(_exception),
                Failed => failure.Invoke(_failure),
                Canceled => cancellation.Invoke(),
                Value => success.Invoke(_value, token),
                _ => error.Invoke(Unexpected.Impossible)
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public UniTask<TMatch> MatchAsync<TMatch>
        (
            Func<TValue, CancellationToken, UniTask<TMatch>> success,
            Func<CancellationToken, UniTask<TMatch>> cancellation,
            Func<Expected.Failure, CancellationToken, UniTask<TMatch>> failure,
            Func<Exception, CancellationToken, UniTask<TMatch>> error,
            CancellationToken token = default
        ) {
            return _income switch
            {
                Exception => error.Invoke(_exception, token),
                Failed => failure.Invoke(_failure, token),
                Canceled => cancellation.Invoke(token),
                Value => success.Invoke(_value, token),
                _ => error.Invoke(Unexpected.Impossible, token)
            };
        }
    }

    [Flags]
    internal enum AsyncRichResultValueIncomeSource
    {
        Value = 1 << 0,
        Canceled = 1 << 1,
        Failed = 1 << 2,
        Exception = 1 << 3
    }
}
