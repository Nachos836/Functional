#nullable enable

using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;

using static System.Runtime.CompilerServices.MethodImplOptions;

namespace Functional.Async
{
    using Core.Outcome;

    using static AsyncResultValueIncomeSource;

    public readonly struct AsyncResult<TValue>
    {
        private static bool IsValueType { get; } = typeof(TValue).IsValueType;

        private readonly AsyncResultValueIncomeSource _income;
        private readonly TValue _value;
        private readonly Exception _exception;

        [MethodImpl(AggressiveInlining)]
        private AsyncResult(TValue value)
        {
            _income = Value;
            _value = value;
            _exception = default!;
        }

        [MethodImpl(AggressiveInlining)]
        private AsyncResult(CancellationToken _)
        {
            _income = Canceled;
            _value = default!;
            _exception = default!;
        }

        [MethodImpl(AggressiveInlining)]
        private AsyncResult(Exception exception)
        {
            _income = Exception;
            _value = default!;
            _exception = exception;
        }

        [Pure] public static AsyncResult<TValue> Cancel { get; } = new (CancellationToken.None);
        [Pure] public static AsyncResult<TValue> Error { get; } = new (Unexpected.Error);
        [Pure] public static AsyncResult<TValue> Impossible { get; } = new (Unexpected.Impossible);

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static implicit operator AsyncResult<TValue> (TValue value)
        {
            if (IsValueType) return new (value);

            return Equals(value, null) is false
                ? new (value)
                : new (new ArgumentNullException(nameof(value)));
        }
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static implicit operator AsyncResult<TValue> (CancellationToken cancellation) => new (cancellation);
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static implicit operator AsyncResult<TValue> (Exception exception)
        {
            return Equals(exception, null) is false
                ? new (exception)
                : new (new ArgumentNullException(nameof(exception)));
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static AsyncResult<TValue> FromResult(TValue value) => value;
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static AsyncResult<TValue> FromCancellation(CancellationToken cancellation) => cancellation;
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static AsyncResult<TValue> FromException(Exception error) => error;

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public AsyncResult<TValue, TAnother> Attach<TAnother>(Func<TValue, AsyncResult<TAnother>> merge)
        {
            return _income switch
            {
                Exception => _exception,
                Canceled => AsyncResult<TValue, TAnother>.Cancel,
                Value => Combine(merge.Invoke(_value)),
                _ => AsyncResult<TValue, TAnother>.Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public AsyncResult<TValue, TAnother> Attach<TAnother>(TAnother another)
        {
            return _income switch
            {
                Exception => _exception,
                Canceled => AsyncResult<TValue, TAnother>.Cancel,
                Value => (_value, another),
                _ => AsyncResult<TValue, TAnother>.Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public AsyncResult<TValue, TFirst, TSecond> Attach<TFirst, TSecond>(TFirst first, TSecond second)
        {
            return _income switch
            {
                Exception => _exception,
                Canceled => AsyncResult<TValue, TFirst, TSecond>.Cancel,
                Value => (_value, first, second),
                _ => AsyncResult<TValue, TFirst, TSecond>.Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public AsyncResult<TValue, TAnother> Combine<TAnother>(AsyncResult<TAnother> another)
        {
            return (_income, another._income) switch
            {
                (Exception, Exception) => new AggregateException(_exception, another._exception),
                (Exception, _) => _exception,
                (_, Exception) => another._exception,
                (Canceled, Value) or (Value, Canceled) => AsyncResult<TValue, TAnother>.Cancel,
                (Value, Value) => (_value, another._value),
                _ => AsyncResult<TValue, TAnother>.Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public AsyncResult Run(Func<TValue, AsyncResult> run)
        {
            return _income switch
            {
                Exception => _exception,
                Canceled => AsyncResult.Cancel,
                Value => run.Invoke(_value),
                _ => AsyncResult.Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public AsyncResult Run
        (
            Func<TValue, CancellationToken, AsyncResult> run,
            CancellationToken cancellation = default
        ) {
            return _income switch
            {
                Exception => _exception,
                Canceled => AsyncResult.Cancel,
                Value => run.Invoke(_value, cancellation),
                _ => AsyncResult.Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public AsyncResult<TAnother> Run<TAnother>(Func<TValue, AsyncResult<TAnother>> run)
        {
            return _income switch
            {
                Exception => _exception,
                Canceled => AsyncResult<TAnother>.Cancel,
                Value => run.Invoke(_value),
                _ => AsyncResult<TAnother>.Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public AsyncResult<TAnother> Run<TAnother>
        (
            Func<TValue, CancellationToken, AsyncResult<TAnother>> run,
            CancellationToken cancellation = default
        ) {
            return _income switch
            {
                Exception => _exception,
                Canceled => AsyncResult<TAnother>.Cancel,
                Value => run.Invoke(_value, cancellation),
                _ => AsyncResult<TAnother>.Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public UniTask<AsyncResult> RunAsync
        (
            Func<TValue, CancellationToken, UniTask<AsyncResult>> run,
            CancellationToken cancellation = default
        ) {
            return _income switch
            {
                Exception => UniTask.FromResult<AsyncResult>(_exception),
                Canceled => UniTask.FromResult(AsyncResult.Cancel),
                Value => run.Invoke(_value, cancellation),
                _ => UniTask.FromResult(AsyncResult.Impossible)
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public UniTask<AsyncResult<TAnother>> RunAsync<TAnother>
        (
            Func<TValue, CancellationToken, UniTask<AsyncResult<TAnother>>> run,
            CancellationToken cancellation = default
        ) {
            return _income switch
            {
                Exception => UniTask.FromResult<AsyncResult<TAnother>>(_exception),
                Canceled => UniTask.FromResult(AsyncResult<TAnother>.Cancel),
                Value => run.Invoke(_value, cancellation),
                _ => UniTask.FromResult(AsyncResult<TAnother>.Impossible)
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public TMatch Match<TMatch>
        (
            Func<TValue, TMatch> success,
            Func<TMatch> cancellation,
            Func<Exception, TMatch> error
        ) {
            return _income switch
            {
                Exception => error.Invoke(_exception),
                Canceled => cancellation.Invoke(),
                Value => success.Invoke(_value),
                _ => error.Invoke(Unexpected.Impossible)
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public TMatch Match<TMatch>
        (
            Func<TValue, CancellationToken, TMatch> success,
            Func<TMatch> cancellation,
            Func<Exception, TMatch> error,
            CancellationToken token = default
        ) {
            return _income switch
            {
                Exception => error.Invoke(_exception),
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
            Func<Exception, UniTask> error,
            CancellationToken token = default
        ) {
            return _income switch
            {
                Exception => error.Invoke(_exception),
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
            Func<Exception, CancellationToken, UniTask<TMatch>> error,
            CancellationToken token = default
        ) {
            return _income switch
            {
                Exception => error.Invoke(_exception, token),
                Canceled => cancellation.Invoke(token),
                Value => success.Invoke(_value, token),
                _ => error.Invoke(Unexpected.Impossible, token)
            };
        }
    }

    public readonly struct AsyncResult<TFirst, TSecond>
    {
        private readonly AsyncResultValueIncomeSource _income;
        private readonly (TFirst First, TSecond Second) _value;
        private readonly Exception _exception;

        [MethodImpl(AggressiveInlining)]
        private AsyncResult(TFirst first, TSecond second)
        {
            _income = Value;
            _value = (first, second);
            _exception = default!;
        }

        [MethodImpl(AggressiveInlining)]
        private AsyncResult(CancellationToken _)
        {
            _income = Canceled;
            _value = default;
            _exception = default!;
        }

        [MethodImpl(AggressiveInlining)]
        private AsyncResult(Exception exception)
        {
            _income = Exception;
            _value = default;
            _exception = exception;
        }

        [Pure] public static AsyncResult<TFirst, TSecond> Error { get; } = new (Unexpected.Error);
        [Pure] public static AsyncResult<TFirst, TSecond> Cancel { get; } = new (CancellationToken.None);
        [Pure] public static AsyncResult<TFirst, TSecond> Impossible { get; } = new (Unexpected.Impossible);

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static implicit operator AsyncResult<TFirst, TSecond> (ValueTuple<TFirst, TSecond> income) => new (income.Item1, income.Item2);
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static implicit operator AsyncResult<TFirst, TSecond> (CancellationToken cancellation) => new (cancellation);
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static implicit operator AsyncResult<TFirst, TSecond> (Exception exception) => new (exception);

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static AsyncResult<TFirst, TSecond> FromResult(TFirst first, TSecond second) => new (first, second);
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static AsyncResult<TFirst, TSecond> FromCancellation(CancellationToken cancellation) => cancellation;
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static AsyncResult<TFirst, TSecond> FromException(Exception error) => error;

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public AsyncResult<TFirst, TSecond, TThird> Attach<TThird>(TThird additional)
        {
            return _income switch
            {
                Exception => _exception,
                Canceled => AsyncResult<TFirst, TSecond, TThird>.Cancel,
                Value => (_value.First, _value.Second, additional),
                _ => AsyncResult<TFirst, TSecond, TThird>.Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public AsyncResult<TFirst> Reduce(Func<TFirst, TSecond, TFirst> reducer)
        {
            return _income switch
            {
                Exception => _exception,
                Canceled => AsyncResult<TFirst>.Cancel,
                Value => reducer.Invoke(_value.First, _value.Second),
                _ => AsyncResult<TFirst>.Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public AsyncResult<TSecond> Reduce(Func<TFirst, TSecond, TSecond> reducer)
        {
            return _income switch
            {
                Exception => _exception,
                Canceled => AsyncResult<TSecond>.Cancel,
                Value => reducer.Invoke(_value.First, _value.Second),
                _ => AsyncResult<TSecond>.Impossible
            };
        }

        [MethodImpl(AggressiveInlining)]
        public void Run(Action<TFirst, TSecond> action)
        {
            if (_income == Value)
            {
                action.Invoke(_value.First, _value.Second);
            }
        }

        [MethodImpl(AggressiveInlining)]
        public void Run
        (
            Action<TFirst, TSecond, CancellationToken> action,
            CancellationToken cancellation = default
        ) {
            if (cancellation.IsCancellationRequested) return;

            if (_income == Value)
            {
                action.Invoke(_value.First, _value.Second, cancellation);
            }
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public AsyncResult Run
        (
            Func<TFirst, TSecond, CancellationToken, AsyncResult> action,
            CancellationToken cancellation = default
        ) {
            return _income switch
            {
                Exception => _exception,
                Canceled => AsyncResult.Cancel,
                Value => action.Invoke(_value.First, _value.Second, cancellation),
                _ => AsyncResult.Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public AsyncResult<TAnother> Run<TAnother>
        (
            Func<TFirst, TSecond, CancellationToken, AsyncResult<TAnother>> action,
            CancellationToken cancellation = default
        ) {
            return _income switch
            {
                Exception => _exception,
                Canceled => AsyncResult<TAnother>.Cancel,
                Value => action.Invoke(_value.First, _value.Second, cancellation),
                _ => AsyncResult<TAnother>.Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public UniTask<AsyncResult> RunAsync
        (
            Func<TFirst, TSecond, CancellationToken, UniTask<AsyncResult>> action,
            CancellationToken cancellation = default
        ) {
            return _income switch
            {
                Exception => UniTask.FromResult<AsyncResult>(_exception),
                Canceled => UniTask.FromResult(AsyncResult.Cancel),
                Value => action.Invoke(_value.First, _value.Second, cancellation),
                _ => UniTask.FromResult(AsyncResult.Impossible)
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public UniTask<AsyncResult<TAnother>> RunAsync<TAnother>
        (
            Func<TFirst, TSecond, CancellationToken, UniTask<AsyncResult<TAnother>>> action,
            CancellationToken cancellation = default
        ) {
            return _income switch
            {
                Exception => UniTask.FromResult<AsyncResult<TAnother>>(_exception),
                Canceled => UniTask.FromResult(AsyncResult<TAnother>.Cancel),
                Value => action.Invoke(_value.First, _value.Second, cancellation),
                _ => UniTask.FromResult(AsyncResult<TAnother>.Impossible)
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public UniTask<AsyncResult<TAnother1, TAnother2>> RunAsync<TAnother1, TAnother2>
        (
            Func<TFirst, TSecond, CancellationToken, UniTask<AsyncResult<TAnother1, TAnother2>>> action,
            CancellationToken cancellation = default
        ) {
            return _income switch
            {
                Exception => UniTask.FromResult<AsyncResult<TAnother1, TAnother2>>(_exception),
                Canceled => UniTask.FromResult(AsyncResult<TAnother1, TAnother2>.Cancel),
                Value => action.Invoke(_value.First, _value.Second, cancellation),
                _ => UniTask.FromResult(AsyncResult<TAnother1, TAnother2>.Impossible)
            };
        }

        [MethodImpl(AggressiveInlining)]
        public void Match
        (
            Action<CancellationToken> success,
            Action<TFirst, TSecond> cancellation,
            Action<Exception> error,
            CancellationToken token = default
        ) {
            switch (_income)
            {
                case Exception:
                    error.Invoke(_exception);
                    return;
                case Canceled:
                    cancellation.Invoke(_value.First, _value.Second);
                    return;
                case Value:
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
            Func<TFirst, TSecond, CancellationToken, TMatch> success,
            Func<TMatch> cancellation,
            Func<Exception, TMatch> error,
            CancellationToken token = default
        ) {
            return _income switch
            {
                Exception => error.Invoke(_exception),
                Canceled => cancellation.Invoke(),
                Value => success.Invoke(_value.First, _value.Second, token),
                _ => error.Invoke(Unexpected.Impossible)
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public UniTask<TMatch> MatchAsync<TMatch>
        (
            Func<TFirst, TSecond, CancellationToken, UniTask<TMatch>> success,
            Func<UniTask<TMatch>> cancellation,
            Func<Exception, UniTask<TMatch>> error,
            CancellationToken token = default
        ) {
            return _income switch
            {
                Exception => error.Invoke(_exception),
                Canceled => cancellation.Invoke(),
                Value => success.Invoke(_value.First, _value.Second, token),
                _ => error.Invoke(Unexpected.Impossible)
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public UniTask<TMatch> MatchAsync<TMatch>
        (
            Func<TFirst, TSecond, CancellationToken, UniTask<TMatch>> success,
            Func<CancellationToken, UniTask<TMatch>> cancellation,
            Func<Exception, CancellationToken, UniTask<TMatch>> error,
            CancellationToken token = default
        ) {
            return _income switch
            {
                Exception => error.Invoke(_exception, token),
                Canceled => cancellation.Invoke(token),
                Value => success.Invoke(_value.First, _value.Second, token),
                _ => error.Invoke(Unexpected.Impossible, token)
            };
        }
    }

    public readonly struct AsyncResult<TFirst, TSecond, TThird>
    {
        private readonly AsyncResultValueIncomeSource _income;
        private readonly (TFirst First, TSecond Second, TThird Third) _value;
        private readonly Exception _exception;

        [MethodImpl(AggressiveInlining)]
        private AsyncResult(TFirst first, TSecond second, TThird third)
        {
            _income = Value;
            _value = (first, second, third);
            _exception = default!;
        }

        [MethodImpl(AggressiveInlining)]
        private AsyncResult(CancellationToken _)
        {
            _income = Canceled;
            _value = default;
            _exception = default!;
        }

        [MethodImpl(AggressiveInlining)]
        private AsyncResult(Exception exception)
        {
            _income = Exception;
            _value = default;
            _exception = exception;
        }

        [Pure] public static AsyncResult<TFirst, TSecond, TThird> Cancel { get; } = new (CancellationToken.None);
        [Pure] public static AsyncResult<TFirst, TSecond, TThird> Error { get; } = new (Unexpected.Error);
        [Pure] public static AsyncResult<TFirst, TSecond, TThird> Impossible { get; } = new (Unexpected.Impossible);

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static implicit operator AsyncResult<TFirst, TSecond, TThird> (ValueTuple<TFirst, TSecond, TThird> income) => new (income.Item1, income.Item2, income.Item3);
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static implicit operator AsyncResult<TFirst, TSecond, TThird> (CancellationToken cancellation) => new (cancellation);
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static implicit operator AsyncResult<TFirst, TSecond, TThird> (Exception error) => new (error);

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static AsyncResult<TFirst, TSecond, TThird> FromResult(TFirst first, TSecond second, TThird third) => new (first, second, third);
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static AsyncResult<TFirst, TSecond, TThird> FromCancellation(CancellationToken cancellation) => cancellation;
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static AsyncResult<TFirst, TSecond, TThird> FromException(Exception error) => error;

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public AsyncResult<TFirst> Reduce(Func<TFirst, TSecond, TThird, TFirst> reducer)
        {
            return _income switch
            {
                Exception => _exception,
                Canceled => AsyncResult<TFirst>.Cancel,
                Value => reducer.Invoke(_value.First, _value.Second, _value.Third),
                _ => AsyncResult<TFirst>.Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public AsyncResult<TSecond> Reduce(Func<TFirst, TSecond, TThird, TSecond> reducer)
        {
            return _income switch
            {
                Exception => _exception,
                Canceled => AsyncResult<TSecond>.Cancel,
                Value => reducer.Invoke(_value.First, _value.Second, _value.Third),
                _ => AsyncResult<TSecond>.Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public AsyncResult<TThird> Reduce(Func<TFirst, TSecond, TThird, TThird> reducer)
        {
            return _income switch
            {
                Exception => _exception,
                Canceled => AsyncResult<TThird>.Cancel,
                Value => reducer.Invoke(_value.First, _value.Second, _value.Third),
                _ => AsyncResult<TThird>.Impossible
            };
        }

        [MethodImpl(AggressiveInlining)]
        public void Run(Action<TFirst, TSecond, TThird> action)
        {
            if (_income == Value)
            {
                action.Invoke(_value.First, _value.Second, _value.Third);
            }
        }

        [MethodImpl(AggressiveInlining)]
        public void Run
        (
            Action<TFirst, TSecond, TThird, CancellationToken> action,
            CancellationToken cancellation = default
        ) {
            if (cancellation.IsCancellationRequested) return;

            if (_income == Value)
            {
                action.Invoke(_value.First, _value.Second, _value.Third, cancellation);
            }
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public AsyncResult Run
        (
            Func<TFirst, TSecond, TThird, CancellationToken, AsyncResult> action,
            CancellationToken cancellation = default
        ) {
            return _income switch
            {
                Exception => _exception,
                Canceled => AsyncResult.Cancel,
                Value => action.Invoke(_value.First, _value.Second, _value.Third, cancellation),
                _ => AsyncResult.Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public AsyncResult<TAnother> Run<TAnother>
        (
            Func<TFirst, TSecond, TThird, CancellationToken, AsyncResult<TAnother>> action,
            CancellationToken cancellation = default
        ) {
            return _income switch
            {
                Exception => _exception,
                Canceled => AsyncResult<TAnother>.Cancel,
                Value => action.Invoke(_value.First, _value.Second, _value.Third, cancellation),
                _ => AsyncResult<TAnother>.Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public UniTask<AsyncResult> RunAsync
        (
            Func<TFirst, TSecond, TThird, CancellationToken, UniTask<AsyncResult>> action,
            CancellationToken cancellation = default
        ) {
            return _income switch
            {
                Exception => UniTask.FromResult<AsyncResult>(_exception),
                Canceled => UniTask.FromResult(AsyncResult.Cancel),
                Value => action.Invoke(_value.First, _value.Second, _value.Third, cancellation),
                _ => UniTask.FromResult(AsyncResult.Impossible)
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public UniTask<AsyncResult<TAnother>> RunAsync<TAnother>
        (
            Func<TFirst, TSecond, TThird, CancellationToken, UniTask<AsyncResult<TAnother>>> action,
            CancellationToken cancellation = default
        ) {
            return _income switch
            {
                Exception => UniTask.FromResult<AsyncResult<TAnother>>(_exception),
                Canceled => UniTask.FromResult(AsyncResult<TAnother>.Cancel),
                Value => action.Invoke(_value.First, _value.Second, _value.Third, cancellation),
                _ => UniTask.FromResult(AsyncResult<TAnother>.Impossible)
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public UniTask<AsyncResult<TAnother1, TAnother2>> RunAsync<TAnother1, TAnother2>
        (
            Func<TFirst, TSecond, TThird, CancellationToken, UniTask<AsyncResult<TAnother1, TAnother2>>> action,
            CancellationToken cancellation = default
        ) {
            return _income switch
            {
                Exception => UniTask.FromResult<AsyncResult<TAnother1, TAnother2>>(_exception),
                Canceled => UniTask.FromResult(AsyncResult<TAnother1, TAnother2>.Cancel),
                Value => action.Invoke(_value.First, _value.Second, _value.Third, cancellation),
                _ => UniTask.FromResult(AsyncResult<TAnother1, TAnother2>.Impossible)
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public UniTask<AsyncResult<TAnother, TAnotherOne, TYetAnother>> RunAsync<TAnother, TAnotherOne, TYetAnother>
        (
            Func<TFirst, TSecond, TThird, CancellationToken, UniTask<AsyncResult<TAnother, TAnotherOne, TYetAnother>>> action,
            CancellationToken cancellation = default
        ) {
            return _income switch
            {
                Exception => UniTask.FromResult<AsyncResult<TAnother, TAnotherOne, TYetAnother>>(_exception),
                Canceled => UniTask.FromResult(AsyncResult<TAnother, TAnotherOne, TYetAnother>.Cancel),
                Value => action.Invoke(_value.First, _value.Second, _value.Third, cancellation),
                _ => UniTask.FromResult(AsyncResult<TAnother, TAnotherOne, TYetAnother>.Impossible)
            };
        }

        [MethodImpl(AggressiveInlining)]
        public void Match
        (
            Action<CancellationToken> success,
            Action<TFirst, TSecond, TThird> cancellation,
            Action<Exception> error,
            CancellationToken token = default
        ) {
            switch (_income)
            {
                case Exception:
                    error.Invoke(_exception);
                    return;
                case Canceled:
                    cancellation.Invoke(_value.First, _value.Second, _value.Third);
                    return;
                case Value:
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
            Func<TFirst, TSecond, TThird, CancellationToken, TMatch> success,
            Func<TMatch> cancellation,
            Func<Exception, TMatch> error,
            CancellationToken token = default
        ) {
            return _income switch
            {
                Exception => error.Invoke(_exception),
                Canceled => cancellation.Invoke(),
                Value => success.Invoke(_value.First, _value.Second, _value.Third, token),
                _ => error.Invoke(Unexpected.Impossible)
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public UniTask<TMatch> MatchAsync<TMatch>
        (
            Func<TFirst, TSecond, TThird, CancellationToken, UniTask<TMatch>> success,
            Func<UniTask<TMatch>> cancellation,
            Func<Exception, UniTask<TMatch>> error,
            CancellationToken token = default
        ) {
            return _income switch
            {
                Exception => error.Invoke(_exception),
                Canceled => cancellation.Invoke(),
                Value => success.Invoke(_value.First, _value.Second, _value.Third, token),
                _ => error.Invoke(Unexpected.Impossible)
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public UniTask<TMatch> MatchAsync<TMatch>
        (
            Func<TFirst, TSecond, TThird, CancellationToken, UniTask<TMatch>> success,
            Func<CancellationToken, UniTask<TMatch>> cancellation,
            Func<Exception, CancellationToken, UniTask<TMatch>> error,
            CancellationToken token = default
        ) {
            return _income switch
            {
                Exception => error.Invoke(_exception, token),
                Canceled => cancellation.Invoke(token),
                Value => success.Invoke(_value.First, _value.Second, _value.Third, token),
                _ => error.Invoke(Unexpected.Impossible, token)
            };
        }
    }

    internal enum AsyncResultValueIncomeSource
    {
        Value = 1,
        Canceled = 2,
        Exception = 3
    }
}
