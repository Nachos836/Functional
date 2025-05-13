using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;

using static System.Runtime.CompilerServices.MethodImplOptions;

namespace Functional.Async
{
    using Core.Outcome;

    using static AsyncRichResultVoidIncomeSource;

    public readonly struct AsyncRichResult
    {
        private readonly AsyncRichResultVoidIncomeSource _income;
        private readonly Expected.Failure _failure;
        private readonly Exception _exception;

        [MethodImpl(AggressiveInlining)]
        private AsyncRichResult(Success _)
        {
            _income = Succeed;
            _failure = default!;
            _exception = default!;
        }

        [MethodImpl(AggressiveInlining)]
        private AsyncRichResult(CancellationToken _)
        {
            _income = Canceled;
            _failure = default!;
            _exception = default!;
        }

        [MethodImpl(AggressiveInlining)]
        private AsyncRichResult(Expected.Failure failure)
        {
            _income = Failed;
            _failure = failure!;
            _exception = default!;
        }

        [MethodImpl(AggressiveInlining)]
        private AsyncRichResult(Exception exception)
        {
            _income = Exception;
            _failure = default!;
            _exception = exception;
        }

        [Pure] public static AsyncRichResult Success { get; } = new (Expected.Success);
        [Pure] public static AsyncRichResult Cancel { get; } = new (CancellationToken.None);
        [Pure] public static AsyncRichResult Failure { get; } = new (Expected.Failed);
        [Pure] public static AsyncRichResult Error { get; } = new (Unexpected.Error);
        [Pure] public static AsyncRichResult Impossible { get; } = new (Unexpected.Impossible);

        [Pure] public bool IsSuccessful => _income == Succeed;
        [Pure] public bool IsCancellation => _income == Canceled;
        [Pure] public bool IsFailure => _income == Failed;
        [Pure] public bool IsError => _income != Succeed && _income != Canceled && _income != Failed;

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static implicit operator AsyncRichResult (CancellationToken cancellation) => new (cancellation);
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static implicit operator AsyncRichResult (Expected.Failure failure) => new (failure);
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static implicit operator AsyncRichResult (Exception error) => new (error);

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static AsyncRichResult FromCancellation(CancellationToken cancellation) => cancellation;
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static AsyncRichResult FromFailure(Expected.Failure failure) => failure;
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static AsyncRichResult FromException(Exception exception) => exception;

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public AsyncResult AsAsyncResult()
        {
            return _income switch
            {
                Exception => _exception,
                Failed => _failure.AsException(),
                Canceled => AsyncResult.Cancel,
                Succeed => AsyncResult.Success,
                _ => AsyncResult.Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public AsyncRichResult<TAnother> Attach<TAnother>(TAnother another)
        {
            return _income switch
            {
                Exception => _exception,
                Failed => _failure,
                Canceled => AsyncRichResult<TAnother>.Cancel,
                Succeed => another,
                _ => AsyncRichResult<TAnother>.Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public AsyncRichResult Combine(AsyncRichResult another)
        {
            return (_income, another._income) switch
            {
                (Exception, Exception) => new AggregateException(_exception, another._exception),
                (Exception, _) => _exception,
                (_, Exception) => another._exception,
                (Failed, _) => _failure,
                (_, Failed) => another._failure,
                (Canceled, Canceled) => Cancel,
                (Canceled, Succeed) => Cancel,
                (Succeed, Canceled) => Cancel,
                (Succeed, Succeed) => this,
                _ => Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public AsyncRichResult Run(Action action)
        {
            if (IsSuccessful)
            {
                action.Invoke();
            }

            return this;
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public AsyncRichResult Run(Func<AsyncRichResult> action)
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
            Func< Expected.Failure, TMatch> failure,
            Func<Exception, TMatch> error,
            CancellationToken token = default
        ) {
            return _income switch
            {
                Exception => error.Invoke(_exception),
                Failed => failure.Invoke(_failure),
                Canceled => cancellation.Invoke(),
                Succeed => success.Invoke(token),
                _ => error.Invoke(Unexpected.Impossible)
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public UniTask MatchAsync
        (
            Func<CancellationToken, UniTask> success,
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
                Succeed => success.Invoke(token),
                _ => error.Invoke(Unexpected.Impossible)
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public UniTask MatchAsync
        (
            Func<CancellationToken, UniTask> success,
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
                Succeed => success.Invoke(token),
                _ => error.Invoke(Unexpected.Impossible, token)
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public UniTask<TMatch> MatchAsync<TMatch>
        (
            Func<CancellationToken, UniTask<TMatch>> success,
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
                Succeed => success.Invoke(token),
                _ => error.Invoke(Unexpected.Impossible)
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public UniTask<TMatch> MatchAsync<TMatch>
        (
            Func<CancellationToken, UniTask<TMatch>> success,
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
                Succeed => success.Invoke(token),
                _ => error.Invoke(Unexpected.Impossible, token)
            };
        }
    }

    [Flags]
    internal enum AsyncRichResultVoidIncomeSource
    {
        Succeed = 1 << 0,
        Canceled = 1 << 1,
        Failed = 1 << 2,
        Exception = 1 << 3
    }
}
